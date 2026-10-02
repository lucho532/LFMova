using LFMova.Application.DTOs.Jornadas;
using LFMova.Application.Implementations.Jornadas;
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
    private readonly INotificacionServicio _notificacionServicio;
    private readonly DepuradorJornada _depurador;
    private readonly EnviadorSoporteRutas _enviadorSoporte;

    /// <summary>Crea el servicio con sus repositorios y colaboradores.</summary>
    public JornadaServicio(
        IJornadaRepositorio jornadaRepositorio,
        IServicioRepositorio servicioRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        INotificacionServicio notificacionServicio,
        DepuradorJornada depurador,
        EnviadorSoporteRutas enviadorSoporte)
    {
        _jornadaRepositorio = jornadaRepositorio;
        _servicioRepositorio = servicioRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _notificacionServicio = notificacionServicio;
        _depurador = depurador;
        _enviadorSoporte = enviadorSoporte;
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

        // Además del aviso en la aplicación, cada conductor recibe por correo un Excel con sus rutas.
        await _enviadorSoporte.EnviarAsync(jornada, serviciosAPublicar);
    }

    /// <inheritdoc />
    public Task<DeshacerRepartoDto> DeshacerRepartoAsync(int empresaId, int jornadaId)
        => _depurador.DeshacerRepartoAsync(empresaId, jornadaId);

    /// <inheritdoc />
    public Task<EliminarRastroDto> EliminarRastroAsync(int empresaId, int jornadaId)
        => _depurador.EliminarRastroAsync(empresaId, jornadaId);

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
