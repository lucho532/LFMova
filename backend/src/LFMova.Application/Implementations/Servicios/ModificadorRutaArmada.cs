using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.Servicios;

/// <summary>
/// Aplica los cambios del coordinador sobre una ruta ya armada que su conductor puede estar viendo:
/// volver a abrirla para edición o eliminarla, avisándole al conductor. No toca rutas en curso o ya
/// realizadas ni decide autorización.
/// </summary>
public class ModificadorRutaArmada
{
    private readonly AccesoServicio _acceso;
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly INotificacionServicio _notificacionServicio;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public ModificadorRutaArmada(
        AccesoServicio acceso,
        IServicioRepositorio servicioRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        INotificacionServicio notificacionServicio)
    {
        _acceso = acceso;
        _servicioRepositorio = servicioRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _notificacionServicio = notificacionServicio;
    }

    /// <summary>Devuelve el servicio a ASIGNADO para que el coordinador lo edite, y avisa a su conductor.</summary>
    public async Task DespublicarAsync(int empresaId, int servicioId)
    {
        var servicio = await _acceso.ObtenerDeLaEmpresaAsync(empresaId, servicioId);
        if (servicio is null)
        {
            throw new InvalidOperationException("El servicio indicado no existe en esta empresa.");
        }

        if (!ReglasEstadoServicio.EsTransicionValida(servicio.Estado, EstadoServicio.ASIGNADO))
        {
            throw new InvalidOperationException($"No se puede volver a editar un servicio en estado {servicio.Estado}.");
        }

        servicio.Estado = EstadoServicio.ASIGNADO;
        await _servicioRepositorio.GuardarCambiosAsync();

        if (servicio.UnidadOperativaId is not null)
        {
            var unidadOperativa = await _unidadOperativaRepositorio.ObtenerPorIdAsync(servicio.UnidadOperativaId.Value);
            var conductor = unidadOperativa is null ? null : await _conductorRepositorio.ObtenerPorIdAsync(unidadOperativa.ConductorId);
            if (conductor is not null)
            {
                await _notificacionServicio.CrearAsync(
                    conductor.UsuarioId,
                    "RUTA_MODIFICADA",
                    "Tu ruta volvió a edición",
                    $"El coordinador volvió a abrir tu {FormatoOperacion.DescribirRuta(servicio)} para hacerle cambios. Te avisaremos cuando esté lista de nuevo.");
            }
        }
    }

    /// <summary>Elimina el servicio con sus pasajeros; si el conductor ya lo veía, le avisa.</summary>
    public async Task EliminarAsync(int empresaId, int servicioId)
    {
        var servicio = await _acceso.ObtenerDeLaEmpresaAsync(empresaId, servicioId);
        if (servicio is null)
        {
            throw new InvalidOperationException("El servicio indicado no existe en esta empresa.");
        }

        if (servicio.Estado is EstadoServicio.EN_CURSO or EstadoServicio.FINALIZADO)
        {
            throw new InvalidOperationException("No se puede eliminar una ruta en curso o ya realizada.");
        }

        var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicioId);
        foreach (var pasajero in pasajeros)
        {
            await _servicioPasajeroRepositorio.EliminarAsync(pasajero);
        }
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        // Solo se avisa al conductor si ya veía la ruta (publicada): una ruta sin publicar nunca le llegó.
        var unidadOperativaId = ReglasEstadoServicio.EsVisibleParaConductorYEmpleado(servicio.Estado) ? servicio.UnidadOperativaId : null;
        var rutaEliminada = FormatoOperacion.DescribirRuta(servicio);
        await _servicioRepositorio.EliminarAsync(servicio);
        await _servicioRepositorio.GuardarCambiosAsync();

        if (unidadOperativaId is not null)
        {
            var unidadOperativa = await _unidadOperativaRepositorio.ObtenerPorIdAsync(unidadOperativaId.Value);
            var conductor = unidadOperativa is null ? null : await _conductorRepositorio.ObtenerPorIdAsync(unidadOperativa.ConductorId);
            if (conductor is not null)
            {
                await _notificacionServicio.CrearAsync(
                    conductor.UsuarioId,
                    "RUTA_MODIFICADA",
                    "Tu ruta fue eliminada",
                    $"El coordinador eliminó tu {rutaEliminada}.");
            }
        }
    }
}
