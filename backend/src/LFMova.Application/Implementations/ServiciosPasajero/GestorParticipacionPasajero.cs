using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.ServiciosPasajero;

/// <summary>
/// Lleva el estado de la participación de un pasajero en su servicio: lo que responde el empleado
/// (confirma o avisa que no asistirá) y lo que registra el conductor durante la ruta (llegada y
/// resultado). Guarda solo el estado actual, sin historial (ver <c>AGENTS.md</c> §18 y §23). No asigna
/// pasajeros a servicios ni los mueve entre rutas.
/// </summary>
public class GestorParticipacionPasajero
{
    private readonly AccesoServicioPasajero _acceso;
    private readonly NotificadorServicioPasajero _notificador;
    private readonly GestorUbicacionPasajero _gestorUbicacion;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IServicioRepositorio _servicioRepositorio;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public GestorParticipacionPasajero(
        AccesoServicioPasajero acceso,
        NotificadorServicioPasajero notificador,
        GestorUbicacionPasajero gestorUbicacion,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IServicioRepositorio servicioRepositorio)
    {
        _acceso = acceso;
        _notificador = notificador;
        _gestorUbicacion = gestorUbicacion;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _servicioRepositorio = servicioRepositorio;
    }

    /// <summary>Marca al pasajero como confirmado, con la dirección nueva si la indicó, y avisa al conductor.</summary>
    public async Task ConfirmarAsync(int empresaId, int servicioPasajeroId, ConfirmarServicioPasajeroDto datos)
    {
        var servicioPasajero = await _acceso.ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);

        if (!string.IsNullOrWhiteSpace(datos.NuevaDireccion))
        {
            if (datos.EstablecerComoHabitual)
            {
                await _gestorUbicacion.ConservarDireccionHabitualComoHistoricaYReemplazarAsync(servicioPasajero.EmpleadoId, datos);
            }

            servicioPasajero.DireccionRecogida = datos.NuevaDireccion;
            servicioPasajero.Latitud = datos.Latitud;
            servicioPasajero.Longitud = datos.Longitud;
        }

        servicioPasajero.Estado = EstadoServicioPasajero.CONFIRMADO;
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        await _notificador.NotificarConductorAsync(servicioPasajero, "PASAJERO_CONFIRMO", "Un pasajero confirmó", "confirmó que asistirá");
    }

    /// <summary>Marca que el pasajero no asistirá y avisa al conductor.</summary>
    public async Task MarcarNoAsistiraAsync(int empresaId, int servicioPasajeroId)
    {
        var servicioPasajero = await _acceso.ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);

        servicioPasajero.Estado = EstadoServicioPasajero.NO_ASISTIRA;
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        await _notificador.NotificarConductorAsync(servicioPasajero, "PASAJERO_NO_ASISTIRA", "Un pasajero no asistirá", "indicó que no asistirá");
    }

    /// <summary>Registra que el conductor llegó al punto de recogida del pasajero y avisa al empleado.</summary>
    public async Task MarcarLlegadaAsync(int empresaId, int servicioPasajeroId)
    {
        var servicioPasajero = await _acceso.ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(servicioPasajero.ServicioId);
        if (servicio is null || servicio.Estado != EstadoServicio.EN_CURSO)
        {
            throw new InvalidOperationException("Solo se puede registrar la llegada mientras el servicio está en curso.");
        }

        if (servicioPasajero.Estado != EstadoServicioPasajero.PROGRAMADO && servicioPasajero.Estado != EstadoServicioPasajero.CONFIRMADO)
        {
            throw new InvalidOperationException($"No se puede registrar la llegada desde el estado {servicioPasajero.Estado}.");
        }

        servicioPasajero.Estado = EstadoServicioPasajero.CONDUCTOR_LLEGO;
        servicioPasajero.HoraLlegadaConductor = DateTime.UtcNow;
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        await _notificador.NotificarEmpleadoAsync(servicioPasajero, "CONDUCTOR_LLEGO", "Tu conductor llegó", "Tu conductor llegó al punto de recogida y te está esperando.");
    }

    /// <summary>Registra el resultado del pasajero (recogido, no contesta, etc.) si la transición es válida.</summary>
    public async Task CambiarEstadoAsync(int empresaId, int servicioPasajeroId, CambiarEstadoServicioPasajeroDto datos)
    {
        var servicioPasajero = await _acceso.ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        if (!ReglasEstadoServicioPasajero.EsTransicionDeResultadoValida(servicioPasajero.Estado, datos.NuevoEstado))
        {
            throw new InvalidOperationException($"No se puede pasar el pasajero de {servicioPasajero.Estado} a {datos.NuevoEstado}.");
        }

        servicioPasajero.Estado = datos.NuevoEstado;
        servicioPasajero.HoraProcesado = DateTime.UtcNow;
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        if (datos.NuevoEstado == EstadoServicioPasajero.RECOGIDO)
        {
            await _gestorUbicacion.GuardarUbicacionSiEsLaPrimeraVezAsync(servicioPasajero);
            await _notificador.NotificarEmpleadoAsync(servicioPasajero, "PASAJERO_RECOGIDO", "Ya vas en camino", "Tu conductor te recogió. ¡Buen viaje!");
        }
    }
}
