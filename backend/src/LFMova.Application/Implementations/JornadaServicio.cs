using LFMova.Application.DTOs.Jornadas;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Application.Mappers;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de administración de jornadas de una empresa.
/// Una jornada no tiene unidad operativa propia: la asignación de unidad es
/// individual por <c>Servicio</c> (ver <see cref="IServicioServicio"/>). No
/// decide autorización: eso se verifica en la capa de Api.
/// </summary>
public class JornadaServicio : IJornadaServicio
{
    private readonly IJornadaRepositorio _jornadaRepositorio;
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly IProgramacionTransporteRepositorio _programacionRepositorio;
    private readonly INotificacionServicio _notificacionServicio;

    /// <summary>Crea el servicio con sus repositorios.</summary>
    public JornadaServicio(
        IJornadaRepositorio jornadaRepositorio,
        IServicioRepositorio servicioRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IProgramacionTransporteRepositorio programacionRepositorio,
        INotificacionServicio notificacionServicio)
    {
        _jornadaRepositorio = jornadaRepositorio;
        _servicioRepositorio = servicioRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _programacionRepositorio = programacionRepositorio;
        _notificacionServicio = notificacionServicio;
    }

    /// <inheritdoc />
    public async Task<JornadaDto> CrearAsync(int empresaId, CrearJornadaDto datos)
    {
        var jornada = new Jornada
        {
            EmpresaId = empresaId,
            FechaOperativa = datos.FechaOperativa
        };

        await _jornadaRepositorio.AgregarAsync(jornada);
        await _jornadaRepositorio.GuardarCambiosAsync();

        return JornadaMapper.AJornadaDto(jornada);
    }

    /// <inheritdoc />
    public async Task<List<JornadaDto>> ObtenerPorEmpresaAsync(int empresaId)
    {
        var jornadas = await _jornadaRepositorio.ObtenerPorEmpresaAsync(empresaId);
        return jornadas.Select(JornadaMapper.AJornadaDto).ToList();
    }

    /// <inheritdoc />
    public async Task PublicarAsync(int empresaId, int jornadaId)
    {
        var jornada = await _jornadaRepositorio.ObtenerPorIdAsync(jornadaId);
        if (jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(jornada, empresaId))
        {
            throw new InvalidOperationException("La jornada indicada no existe en esta empresa.");
        }

        var servicios = await _servicioRepositorio.ObtenerPorJornadaAsync(jornadaId);
        var noCancelados = servicios.Where(s => s.Estado != EstadoServicio.CANCELADO).ToList();

        var sinResolver = noCancelados.Any(s => s.Estado is EstadoServicio.BORRADOR or EstadoServicio.PENDIENTE_ASIGNACION);
        if (sinResolver)
        {
            throw new InvalidOperationException(
                "No se puede publicar la jornada: existen servicios sin resolución de asignación.");
        }

        var sinUnidad = noCancelados.Any(s => s.UnidadOperativaId is null);
        if (sinUnidad)
        {
            throw new InvalidOperationException(
                "No se puede publicar la jornada: existen servicios sin unidad operativa asignada.");
        }

        var serviciosAPublicar = noCancelados.Where(s => s.Estado == EstadoServicio.ASIGNADO).ToList();
        foreach (var servicioAPublicar in serviciosAPublicar)
        {
            servicioAPublicar.Estado = EstadoServicio.PUBLICADO;
        }

        await _servicioRepositorio.GuardarCambiosAsync();

        foreach (var servicioPublicado in serviciosAPublicar)
        {
            await NotificarPublicacionAsync(servicioPublicado);
        }
    }

    /// <inheritdoc />
    public async Task<DeshacerRepartoDto> DeshacerRepartoAsync(int empresaId, int jornadaId)
    {
        var jornada = await _jornadaRepositorio.ObtenerPorIdAsync(jornadaId);
        if (jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(jornada, empresaId))
        {
            throw new InvalidOperationException("La jornada indicada no existe en esta empresa.");
        }

        var servicios = await _servicioRepositorio.ObtenerPorJornadaAsync(jornadaId);
        var aDeshacer = servicios.Where(s => s.Estado is EstadoServicio.BORRADOR or EstadoServicio.PENDIENTE_ASIGNACION or EstadoServicio.ASIGNADO).ToList();

        var resultado = new DeshacerRepartoDto();
        foreach (var servicio in aDeshacer)
        {
            var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicio.ServicioId);
            foreach (var pasajero in pasajeros)
            {
                await _servicioPasajeroRepositorio.EliminarAsync(pasajero);
                resultado.PasajerosLiberados++;
            }

            await _servicioRepositorio.EliminarAsync(servicio);
            resultado.ServiciosEliminados++;
        }

        // Ambos repositorios comparten el mismo contexto de datos: un solo guardado persiste tanto
        // los pasajeros como los servicios eliminados, en el orden correcto según sus relaciones.
        await _servicioRepositorio.GuardarCambiosAsync();

        return resultado;
    }

    /// <inheritdoc />
    public async Task<EliminarRastroDto> EliminarRastroAsync(int empresaId, int jornadaId)
    {
        var jornada = await _jornadaRepositorio.ObtenerPorIdAsync(jornadaId);
        if (jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(jornada, empresaId))
        {
            throw new InvalidOperationException("La jornada indicada no existe en esta empresa.");
        }

        var servicios = await _servicioRepositorio.ObtenerPorJornadaAsync(jornadaId);
        var protegidos = servicios.Where(s => s.Estado is EstadoServicio.PUBLICADO or EstadoServicio.EN_CURSO or EstadoServicio.FINALIZADO).ToList();
        if (protegidos.Count > 0)
        {
            throw new InvalidOperationException(
                "No se puede eliminar: esta jornada ya tiene servicios publicados, en curso o finalizados, que son operación real y no se pueden deshacer.");
        }

        var resultado = new EliminarRastroDto();

        var aDeshacer = servicios.Where(s => s.Estado is EstadoServicio.BORRADOR or EstadoServicio.PENDIENTE_ASIGNACION or EstadoServicio.ASIGNADO).ToList();
        foreach (var servicio in aDeshacer)
        {
            var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicio.ServicioId);
            foreach (var pasajero in pasajeros)
            {
                await _servicioPasajeroRepositorio.EliminarAsync(pasajero);
                resultado.PasajerosEliminados++;
            }

            await _servicioRepositorio.EliminarAsync(servicio);
            resultado.ServiciosEliminados++;
        }

        // Además de los servicios y pasajeros, se eliminan las ProgramacionTransporte de esta fecha que
        // hayan quedado sin ningún ServicioPasajero (por ejemplo, filas de una importación que falló a
        // medias): esas son "rastro" de la importación que impedirían reconocer la cédula como nueva en
        // un reimport desde cero. Se consideran las dos fechas posibles de una importación (ver
        // ImportacionExcelServicio.EjecutarAsync): la fecha operativa y el día siguiente, para las filas
        // de un horario que cruza medianoche.
        var fechas = new[] { jornada.FechaOperativa, jornada.FechaOperativa.AddDays(1) };
        var programacionesDeLaFecha = (await _programacionRepositorio.ObtenerPorEmpresaAsync(empresaId))
            .Where(p => fechas.Contains(p.Fecha))
            .ToList();
        foreach (var programacion in programacionesDeLaFecha)
        {
            // Si todavía tiene un pasajero de servicio (de un servicio protegido que no se tocó, como uno
            // cancelado con historial), no se elimina: violaría la relación con ese ServicioPasajero.
            if (await _servicioPasajeroRepositorio.ObtenerPorProgramacionAsync(programacion.ProgramacionTransporteId) is not null)
            {
                continue;
            }

            await _programacionRepositorio.EliminarAsync(programacion);
            resultado.ProgramacionesEliminadas++;
        }

        await _servicioRepositorio.GuardarCambiosAsync();

        // Si no quedó ningún servicio (ni siquiera uno cancelado), la jornada también se elimina: así una
        // nueva importación de esa fecha arma una jornada nueva, como si nunca se hubiera importado nada.
        var serviciosRestantes = await _servicioRepositorio.ObtenerPorJornadaAsync(jornadaId);
        if (serviciosRestantes.Count == 0)
        {
            await _jornadaRepositorio.EliminarAsync(jornada);
            await _jornadaRepositorio.GuardarCambiosAsync();
            resultado.JornadaEliminada = true;
        }

        return resultado;
    }

    /// <summary>
    /// Notifica al conductor de la unidad asignada y a cada empleado
    /// participante de que el servicio quedó publicado (ver <c>spec.md</c>
    /// §20 y <c>tasks.md</c> T073/T075).
    /// </summary>
    private async Task NotificarPublicacionAsync(Servicio servicio)
    {
        if (servicio.UnidadOperativaId is not null)
        {
            var unidadOperativa = await _unidadOperativaRepositorio.ObtenerPorIdAsync(servicio.UnidadOperativaId.Value);
            var conductor = unidadOperativa is null
                ? null
                : await _conductorRepositorio.ObtenerPorIdAsync(unidadOperativa.ConductorId);

            if (conductor is not null)
            {
                await _notificacionServicio.CrearAsync(
                    conductor.UsuarioId,
                    "SERVICIO_PUBLICADO",
                    "Nueva ruta publicada",
                    $"Ya puedes ver tu {FormatoOperacion.DescribirRuta(servicio)}.");
            }
        }

        var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicio.ServicioId);
        foreach (var pasajero in pasajeros)
        {
            var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(pasajero.EmpleadoId);
            if (empleado is null)
            {
                continue;
            }

            await _notificacionServicio.CrearAsync(
                empleado.UsuarioId,
                "SERVICIO_PUBLICADO",
                "Tu transporte fue publicado",
                $"Ya está publicada tu {FormatoOperacion.DescribirRuta(servicio)}.");
        }
    }
}
