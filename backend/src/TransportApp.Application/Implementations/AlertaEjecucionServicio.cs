using TransportApp.Application.Interfaces;
using TransportApp.Application.Utils;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Domain.Rules;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa la alerta de ruta no iniciada (ver <c>plan.md</c> §38 y
/// <c>tasks.md</c> T094). Desde una hora antes de una ruta de entrada
/// publicada y mientras siga sin iniciarse (hasta el límite de
/// <see cref="ReglasAlertaEjecucion.MinutosMaximoRetraso"/>), avisa en cada
/// verificación al conductor responsable y a los demás conductores de la
/// misma empresa que tienen ruta en ese mismo horario, para que se
/// comuniquen entre ellos. Los textos dicen quién es el conductor, de qué
/// ruta se trata (tipo, hora, sede y fecha) y cuánto falta o cuánto retraso
/// lleva, nunca solo códigos internos. No es una orden de inicio ni
/// modifica el servicio, y no notifica al coordinador.
/// </summary>
public class AlertaEjecucionServicio : IAlertaEjecucionServicio
{
    /// <summary>Tipo de notificación de la alerta.</summary>
    public const string TipoNotificacion = "ALERTA_RUTA_NO_INICIADA";

    /// <summary>Tiempo mínimo entre dos avisos de la misma ruta a la misma persona.</summary>
    public static readonly TimeSpan IntervaloMinimoEntreAvisos = TimeSpan.FromMinutes(10);

    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly INotificacionServicio _notificacionServicio;

    /// <summary>Crea el servicio con sus repositorios.</summary>
    public AlertaEjecucionServicio(
        IServicioRepositorio servicioRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        INotificacionServicio notificacionServicio)
    {
        _servicioRepositorio = servicioRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _notificacionServicio = notificacionServicio;
    }

    /// <inheritdoc />
    public async Task VerificarRutasNoIniciadasAsync()
    {
        var servicios = await _servicioRepositorio.ObtenerPublicadosPorTipoAsync(TipoServicio.ENTRADA);
        var ahoraColombia = ReglasHoraColombia.AhoraColombia(DateTime.UtcNow);

        foreach (var servicio in servicios)
        {
            if (servicio.UnidadOperativaId is null ||
                !ReglasAlertaEjecucion.DebeAlertarRutaNoIniciada(servicio.Fecha, servicio.HoraProgramada, ahoraColombia))
            {
                continue;
            }

            var conductor = await ObtenerConductorAsync(servicio.UnidadOperativaId.Value);
            if (conductor is null)
            {
                continue;
            }

            var companeros = await ObtenerCompanerosDeHorarioAsync(servicio, conductor.ConductorId);
            var ruta = FormatoOperacion.DescribirRuta(servicio);
            var tiempo = DescribirTiempo(ReglasAlertaEjecucion.MinutosParaInicio(servicio.Fecha, servicio.HoraProgramada, ahoraColombia));

            var avisoCompaneros = companeros.Count > 0 ? " Tus compañeros de ese horario también reciben este aviso." : string.Empty;
            await NotificarSinRepetirAsync(
                conductor.UsuarioId,
                $"Todavía no has iniciado tu {ruta}",
                "Tu ruta todavía no ha iniciado",
                $"Todavía no has iniciado tu {ruta}: {tiempo}.{avisoCompaneros}",
                servicio.Jornada is null ? null : $"/conductor/servicios/{servicio.Jornada.EmpresaId}/{servicio.JornadaId}/{servicio.ServicioId}");

            var nombre = FormatoOperacion.NombrePropio(conductor.NombreCompleto);
            var telefono = string.IsNullOrWhiteSpace(conductor.Telefono) ? string.Empty : $" Su teléfono: {conductor.Telefono}.";
            foreach (var companero in companeros)
            {
                await NotificarSinRepetirAsync(
                    companero.UsuarioId,
                    $"{nombre} no ha iniciado la {ruta}",
                    "Un compañero no ha iniciado su ruta",
                    $"{nombre} no ha iniciado la {ruta}: {tiempo}. Comunícate para coordinar.{telefono}",
                    enlace: null);
            }
        }
    }

    /// <summary>"empieza en 30 min", "ya es la hora de inicio" o "lleva 20 min de retraso".</summary>
    private static string DescribirTiempo(int minutosParaInicio)
        => minutosParaInicio > 0 ? $"empieza en {minutosParaInicio} min"
            : minutosParaInicio == 0 ? "ya es la hora de inicio"
            : $"lleva {-minutosParaInicio} min de retraso";

    private async Task<Conductor?> ObtenerConductorAsync(int unidadOperativaId)
    {
        var unidadOperativa = await _unidadOperativaRepositorio.ObtenerPorIdAsync(unidadOperativaId);
        return unidadOperativa is null ? null : await _conductorRepositorio.ObtenerPorIdAsync(unidadOperativa.ConductorId);
    }

    /// <summary>
    /// Conductores de la misma empresa con otra ruta publicada o en curso a la
    /// misma fecha y hora (sin repetir y sin incluir al conductor de la ruta).
    /// </summary>
    private async Task<List<Conductor>> ObtenerCompanerosDeHorarioAsync(Servicio servicio, int conductorId)
    {
        var empresaId = servicio.Jornada?.EmpresaId;
        var mismosHorario = (await _servicioRepositorio.ObtenerActivosPorFechaYHoraAsync(servicio.Fecha, servicio.HoraProgramada))
            .Where(s => s.ServicioId != servicio.ServicioId && s.UnidadOperativaId is not null && s.Jornada?.EmpresaId == empresaId);

        var companeros = new List<Conductor>();
        foreach (var otro in mismosHorario)
        {
            var companero = await ObtenerConductorAsync(otro.UnidadOperativaId!.Value);
            if (companero is not null && companero.ConductorId != conductorId && companeros.All(c => c.ConductorId != companero.ConductorId))
            {
                companeros.Add(companero);
            }
        }

        return companeros;
    }

    /// <summary>
    /// El aviso se repite en cada verificación (cada 15 minutos) mientras la
    /// ruta siga sin iniciarse, pero no se envía de nuevo a la misma persona
    /// sobre la misma ruta si ya lo recibió hace menos de
    /// <see cref="IntervaloMinimoEntreAvisos"/> (por ejemplo, si el API se
    /// reinicia y verifica antes de tiempo). <paramref name="clave"/> es el
    /// fragmento del mensaje que identifica de qué ruta (y de qué conductor)
    /// se trata, sin la parte del tiempo, que cambia en cada aviso.
    /// </summary>
    private async Task NotificarSinRepetirAsync(int usuarioId, string clave, string titulo, string mensaje, string? enlace)
    {
        var limite = DateTime.UtcNow - IntervaloMinimoEntreAvisos;
        var existentes = await _notificacionServicio.ObtenerPorUsuarioAsync(usuarioId);
        if (existentes.Any(n => n.Tipo == TipoNotificacion && n.FechaHora > limite && n.Mensaje.Contains(clave)))
        {
            return;
        }

        await _notificacionServicio.CrearAsync(usuarioId, TipoNotificacion, titulo, mensaje, enlace);
    }
}
