using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;

namespace LFMova.Application.Implementations.ServiciosPasajero;

/// <summary>
/// Avisa al conductor o al empleado de lo que ocurre con un pasajero de un servicio (confirmó, no
/// asistirá, el conductor llegó, etc.). Solo resuelve a quién avisar y arma el mensaje: no cambia el
/// estado del pasajero ni decide cuándo corresponde avisar.
/// </summary>
public class NotificadorServicioPasajero
{
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly INotificacionServicio _notificacionServicio;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public NotificadorServicioPasajero(
        IServicioRepositorio servicioRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        INotificacionServicio notificacionServicio)
    {
        _servicioRepositorio = servicioRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _notificacionServicio = notificacionServicio;
    }

    /// <summary>Avisa al conductor del servicio de lo que hizo el pasajero; no hace nada si el servicio no tiene conductor.</summary>
    public async Task NotificarConductorAsync(ServicioPasajero servicioPasajero, string tipo, string titulo, string accion)
    {
        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(servicioPasajero.ServicioId);
        if (servicio?.UnidadOperativaId is null)
        {
            return;
        }

        var unidadOperativa = await _unidadOperativaRepositorio.ObtenerPorIdAsync(servicio.UnidadOperativaId.Value);
        if (unidadOperativa is null)
        {
            return;
        }

        var conductor = await _conductorRepositorio.ObtenerPorIdAsync(unidadOperativa.ConductorId);
        if (conductor is null)
        {
            return;
        }

        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(servicioPasajero.EmpleadoId);
        await _notificacionServicio.CrearAsync(
            conductor.UsuarioId,
            tipo,
            titulo,
            $"{empleado?.NombreCompleto ?? "El pasajero"} {accion} en el servicio del {servicio.Fecha:yyyy-MM-dd} a las {servicio.HoraProgramada:HH\\:mm}.",
            servicio.Jornada is null ? null : $"/conductor/servicios/{servicio.Jornada.EmpresaId}/{servicio.JornadaId}/{servicio.ServicioId}/chat/{servicioPasajero.ServicioPasajeroId}");
    }

    /// <summary>Avisa al empleado que es pasajero del servicio; no hace nada si el empleado ya no existe.</summary>
    public async Task NotificarEmpleadoAsync(ServicioPasajero servicioPasajero, string tipo, string titulo, string mensaje)
    {
        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(servicioPasajero.EmpleadoId);
        if (empleado is null)
        {
            return;
        }

        await _notificacionServicio.CrearAsync(empleado.UsuarioId, tipo, titulo, mensaje, "/mi-transporte");
    }
}
