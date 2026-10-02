using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;

namespace LFMova.Application.Implementations.ServiciosPasajero;

/// <summary>
/// Gestiona el punto de recogida de un pasajero: la ubicación que comparte en vivo, la que el conductor
/// guarda para próximas veces y el historial de ubicaciones del empleado. Nunca modifica la dirección
/// de servicios anteriores (ver <c>AGENTS.md</c> §17) ni cambia el estado del pasajero.
/// </summary>
public class GestorUbicacionPasajero
{
    private readonly AccesoServicioPasajero _acceso;
    private readonly NotificadorServicioPasajero _notificador;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly IUbicacionRecogidaHistoricaRepositorio _ubicacionHistoricaRepositorio;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public GestorUbicacionPasajero(
        AccesoServicioPasajero acceso,
        NotificadorServicioPasajero notificador,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IUbicacionRecogidaHistoricaRepositorio ubicacionHistoricaRepositorio)
    {
        _acceso = acceso;
        _notificador = notificador;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _ubicacionHistoricaRepositorio = ubicacionHistoricaRepositorio;
    }

    /// <summary>Guarda en el pasajero el punto que compartió en vivo y avisa al conductor.</summary>
    public async Task CompartirUbicacionAsync(int empresaId, int servicioPasajeroId, CompartirUbicacionDto datos)
    {
        var servicioPasajero = await _acceso.ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        servicioPasajero.Latitud = datos.Latitud;
        servicioPasajero.Longitud = datos.Longitud;
        // Se guarda además aparte, con su hora: Latitud/Longitud también las escribe el conductor al
        // guardar el punto de recogida, y así no se sabría cuál de los dos lo puso.
        servicioPasajero.LatitudCompartida = datos.Latitud;
        servicioPasajero.LongitudCompartida = datos.Longitud;
        servicioPasajero.FechaHoraUbicacionCompartida = DateTime.UtcNow;
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        // Sin esta notificación, la pantalla del conductor solo se enteraría en el siguiente sondeo (hasta
        // 15 s después): si mientras tanto le da a "Navegar", el botón usaría un punto desactualizado.
        await _notificador.NotificarConductorAsync(servicioPasajero, "UBICACION_COMPARTIDA", "Ubicación compartida", "compartió su ubicación en vivo");
    }

    /// <summary>Guarda el punto indicado en el pasajero y lo agrega al historial de ubicaciones del empleado.</summary>
    public async Task GuardarUbicacionRecogidaAsync(int empresaId, int servicioPasajeroId, CompartirUbicacionDto datos)
    {
        var servicioPasajero = await _acceso.ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(servicioPasajero.EmpleadoId)
            ?? throw new InvalidOperationException("El empleado indicado no existe.");

        servicioPasajero.Latitud = datos.Latitud;
        servicioPasajero.Longitud = datos.Longitud;

        await _ubicacionHistoricaRepositorio.AgregarAsync(new UbicacionRecogidaHistorica
        {
            EmpleadoId = empleado.EmpleadoId,
            Direccion = servicioPasajero.DireccionRecogida,
            Barrio = empleado.Barrio,
            Latitud = datos.Latitud,
            Longitud = datos.Longitud,
            FechaRegistro = DateTime.UtcNow
        });

        await _ubicacionHistoricaRepositorio.GuardarCambiosAsync();
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();
    }

    /// <summary>Devuelve las ubicaciones guardadas antes para el empleado de ese pasajero.</summary>
    public async Task<List<UbicacionAnteriorDto>> ObtenerUbicacionesAnterioresAsync(int empresaId, int servicioPasajeroId)
    {
        var servicioPasajero = await _acceso.ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        var historicas = await _ubicacionHistoricaRepositorio.ObtenerPorEmpleadoAsync(servicioPasajero.EmpleadoId);
        return historicas
            .Select(u => new UbicacionAnteriorDto { Direccion = u.Direccion, Barrio = u.Barrio, Latitud = u.Latitud, Longitud = u.Longitud, FechaRegistro = u.FechaRegistro })
            .ToList();
    }

    /// <summary>Borra las ubicaciones guardadas del empleado de ese pasajero.</summary>
    public async Task EliminarUbicacionGuardadaAsync(int empresaId, int servicioPasajeroId)
    {
        var servicioPasajero = await _acceso.ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        await _ubicacionHistoricaRepositorio.EliminarPorEmpleadoAsync(servicioPasajero.EmpleadoId);
        await _ubicacionHistoricaRepositorio.GuardarCambiosAsync();
    }

    /// <summary>
    /// Si el empleado todavía no tiene ninguna ubicación guardada y este
    /// pasajero sí tiene un punto GPS conocido (compartido por él o guardado
    /// antes por el conductor), lo deja registrado como su ubicación de
    /// recogida habitual, sin que el conductor tenga que guardarlo a mano.
    /// </summary>
    public async Task GuardarUbicacionSiEsLaPrimeraVezAsync(ServicioPasajero servicioPasajero)
    {
        if (servicioPasajero.Latitud is null || servicioPasajero.Longitud is null)
        {
            return;
        }

        var yaTieneUbicacionGuardada = (await _ubicacionHistoricaRepositorio.ObtenerPorEmpleadoAsync(servicioPasajero.EmpleadoId)).Count > 0;
        if (yaTieneUbicacionGuardada)
        {
            return;
        }

        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(servicioPasajero.EmpleadoId);
        await _ubicacionHistoricaRepositorio.AgregarAsync(new UbicacionRecogidaHistorica
        {
            EmpleadoId = servicioPasajero.EmpleadoId,
            Direccion = servicioPasajero.DireccionRecogida,
            Barrio = empleado?.Barrio ?? string.Empty,
            Latitud = servicioPasajero.Latitud,
            Longitud = servicioPasajero.Longitud,
            FechaRegistro = DateTime.UtcNow
        });
        await _ubicacionHistoricaRepositorio.GuardarCambiosAsync();
    }

    /// <summary>
    /// Reemplaza la dirección habitual del empleado por la nueva que indicó al confirmar, dejando la
    /// anterior en su historial de ubicaciones.
    /// </summary>
    public async Task ConservarDireccionHabitualComoHistoricaYReemplazarAsync(int empleadoId, ConfirmarServicioPasajeroDto datos)
    {
        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(empleadoId);
        if (empleado is null)
        {
            throw new InvalidOperationException("El empleado indicado no existe.");
        }

        var ubicacionAnterior = new UbicacionRecogidaHistorica
        {
            EmpleadoId = empleado.EmpleadoId,
            Direccion = empleado.Direccion,
            Barrio = empleado.Barrio,
            FechaRegistro = DateTime.UtcNow
        };

        await _ubicacionHistoricaRepositorio.AgregarAsync(ubicacionAnterior);

        empleado.Direccion = datos.NuevaDireccion!;
        empleado.Barrio = datos.NuevoBarrio ?? empleado.Barrio;

        await _ubicacionHistoricaRepositorio.GuardarCambiosAsync();
    }
}
