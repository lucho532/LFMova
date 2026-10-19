using LFMova.Application.Implementations.Servicios;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa el cambio de sede de una ruta ya creada. Hace todas las validaciones antes de modificar nada, de modo
/// que un cambio rechazado no deja la ruta a medias. No decide autorización de rol y no toca rutas en curso,
/// realizadas ni canceladas.
/// </summary>
public class CambioSedeServicio : ICambioSedeServicio
{
    private readonly AccesoServicio _acceso;
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly ISedeRepositorio _sedeRepositorio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IProgramacionTransporteRepositorio _programacionRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly INotificacionServicio _notificacionServicio;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public CambioSedeServicio(
        AccesoServicio acceso,
        IServicioRepositorio servicioRepositorio,
        ISedeRepositorio sedeRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IProgramacionTransporteRepositorio programacionRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        INotificacionServicio notificacionServicio)
    {
        _acceso = acceso;
        _servicioRepositorio = servicioRepositorio;
        _sedeRepositorio = sedeRepositorio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _programacionRepositorio = programacionRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _notificacionServicio = notificacionServicio;
    }

    /// <inheritdoc />
    public async Task CambiarSedeAsync(int empresaId, int servicioId, int sedeId)
    {
        var servicio = await _acceso.ObtenerDeLaEmpresaAsync(empresaId, servicioId)
            ?? throw new InvalidOperationException("El servicio indicado no existe en esta empresa.");

        if (!ReglasEstadoServicio.AdmitePasajerosNuevos(servicio.Estado))
        {
            throw new InvalidOperationException("No se puede cambiar la sede de una ruta en curso, realizada o cancelada.");
        }

        var sede = await _sedeRepositorio.ObtenerPorIdAsync(sedeId);
        if (sede is null || sede.EmpresaId != empresaId || !sede.Activa)
        {
            throw new InvalidOperationException("La sede indicada no existe o no está activa en esta empresa.");
        }

        if (sede.SedeId == servicio.SedeId)
        {
            return;
        }

        await AsegurarQueNoSeCruzaLaUnidadAsync(servicio, sede.SedeId);
        var programaciones = await ObtenerProgramacionesAsync(servicioId);
        await AsegurarQueNingunPasajeroChocaAsync(servicio, sede, programaciones);

        var estabaPublicado = servicio.Estado == EstadoServicio.PUBLICADO;
        servicio.SedeId = sede.SedeId;
        servicio.Sede = sede;
        foreach (var programacion in programaciones)
        {
            programacion.SedeId = sede.SedeId;
        }

        // Una ruta publicada que cambia vuelve a ASIGNADO, igual que cuando recibe pasajeros nuevos: el conductor deja
        // de verla hasta que el coordinador la revise y la publique de nuevo.
        if (estabaPublicado)
        {
            servicio.Estado = EstadoServicio.ASIGNADO;
        }

        await _servicioRepositorio.GuardarCambiosAsync();

        if (estabaPublicado)
        {
            await AvisarAlConductorAsync(servicio);
        }
    }

    /// <summary>Rechaza el cambio si dejaría a la unidad del servicio cruzada con una ruta suya con la que antes no se cruzaba.</summary>
    private async Task AsegurarQueNoSeCruzaLaUnidadAsync(Servicio servicio, int sedeNuevaId)
    {
        if (servicio.UnidadOperativaId is null)
        {
            return;
        }

        var otras = (await _servicioRepositorio.ObtenerPorUnidadOperativaAsync(servicio.UnidadOperativaId.Value))
            .Where(s => s.ServicioId != servicio.ServicioId && s.Estado is not (EstadoServicio.CANCELADO or EstadoServicio.FINALIZADO));
        bool SeCruza(Servicio otra, int sedeId) =>
            ReglasUnidadOperativa.SeCruzan(otra.Fecha, otra.HoraProgramada, otra.Tipo, otra.SedeId, servicio.Fecha, servicio.HoraProgramada, servicio.Tipo, sedeId);

        // Solo cuentan los cruces nuevos: las rutas dobles de un mismo vehículo que ya venían del archivo no impiden corregir su sede.
        if (otras.Any(otra => SeCruza(otra, sedeNuevaId) && !SeCruza(otra, servicio.SedeId)))
        {
            throw new InvalidOperationException("Con esa sede, la unidad de esta ruta quedaría con otra ruta suya a la misma hora. Cambia primero su conductor.");
        }
    }

    private async Task<List<ProgramacionTransporte>> ObtenerProgramacionesAsync(int servicioId)
    {
        var programaciones = new List<ProgramacionTransporte>();
        foreach (var pasajero in await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicioId))
        {
            var programacion = await _programacionRepositorio.ObtenerPorIdAsync(pasajero.ProgramacionTransporteId);
            if (programacion is not null)
            {
                programaciones.Add(programacion);
            }
        }

        return programaciones;
    }

    /// <summary>Rechaza el cambio si algún pasajero ya tiene, en la sede nueva, otra programación a esa misma fecha, hora y tipo.</summary>
    private async Task AsegurarQueNingunPasajeroChocaAsync(Servicio servicio, Sede sede, List<ProgramacionTransporte> programaciones)
    {
        foreach (var programacion in programaciones)
        {
            var existente = await _programacionRepositorio.ObtenerPorClaveAsync(programacion.EmpleadoId, sede.SedeId, programacion.Fecha, programacion.Hora, programacion.Tipo);
            if (existente is not null && existente.ProgramacionTransporteId != programacion.ProgramacionTransporteId)
            {
                throw new InvalidOperationException($"Un pasajero de esta ruta ya tiene otra programación en la sede {sede.Nombre} a la misma hora: resuélvelo antes de cambiar la sede.");
            }
        }
    }

    private async Task AvisarAlConductorAsync(Servicio servicio)
    {
        if (servicio.UnidadOperativaId is null)
        {
            return;
        }

        var unidad = await _unidadOperativaRepositorio.ObtenerPorIdAsync(servicio.UnidadOperativaId.Value);
        var conductor = unidad is null ? null : await _conductorRepositorio.ObtenerPorIdAsync(unidad.ConductorId);
        if (conductor is not null)
        {
            await _notificacionServicio.CrearAsync(
                conductor.UsuarioId,
                "RUTA_MODIFICADA",
                "Tu ruta cambió de sede",
                $"El coordinador cambió la sede de tu {FormatoOperacion.DescribirRuta(servicio)}. Te avisaremos cuando esté lista de nuevo.");
        }
    }
}
