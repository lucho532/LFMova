using LFMova.Application.Implementations;
using LFMova.Application.Implementations.ServiciosPasajero;
using LFMova.Application.Interfaces;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Arma un <see cref="ServicioPasajeroServicio"/> real con sus colaboradores a partir de los
/// repositorios y servicios que cada prueba decide (normalmente falsos en memoria), igual que lo
/// haría el contenedor de dependencias. No contiene aserciones ni datos de prueba.
/// </summary>
internal static class ServicioPasajeroServicioFabrica
{
    /// <summary>Crea el servicio conectando todos sus colaboradores con las dependencias indicadas.</summary>
    public static ServicioPasajeroServicio Crear(
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IProgramacionTransporteRepositorio programacionRepositorio,
        IServicioRepositorio servicioRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IUbicacionRecogidaHistoricaRepositorio ubicacionHistoricaRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        INotificacionServicio notificacionServicio,
        IServicioServicio servicioServicio,
        IZonaRepositorio zonaRepositorio,
        ICorredorVialRepositorio corredorVialRepositorio,
        IBarreraGeograficaRepositorio barreraGeograficaRepositorio)
    {
        var acceso = new AccesoServicioPasajero(servicioPasajeroRepositorio, servicioRepositorio);
        var consultas = new ConsultasServicioPasajero(
            acceso, servicioPasajeroRepositorio, servicioRepositorio, empleadoRepositorio, unidadOperativaRepositorio, conductorRepositorio);
        var notificador = new NotificadorServicioPasajero(
            servicioRepositorio, unidadOperativaRepositorio, conductorRepositorio, empleadoRepositorio, notificacionServicio);
        var gestorUbicacion = new GestorUbicacionPasajero(
            acceso, notificador, servicioPasajeroRepositorio, empleadoRepositorio, ubicacionHistoricaRepositorio);
        var gestorParticipacion = new GestorParticipacionPasajero(
            acceso, notificador, gestorUbicacion, servicioPasajeroRepositorio, servicioRepositorio);
        var aprendizCorredor = new AprendizCorredorVial(
            servicioPasajeroRepositorio, empleadoRepositorio, zonaRepositorio, corredorVialRepositorio, barreraGeograficaRepositorio);
        var reorganizador = new ReorganizadorPasajeros(
            acceso, aprendizCorredor, servicioPasajeroRepositorio, servicioRepositorio, servicioServicio);

        return new ServicioPasajeroServicio(
            servicioPasajeroRepositorio, programacionRepositorio, acceso, consultas, gestorParticipacion, gestorUbicacion, reorganizador);
    }
}
