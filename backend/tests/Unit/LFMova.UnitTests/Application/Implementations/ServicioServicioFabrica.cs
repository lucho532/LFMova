using LFMova.Application.Implementations;
using LFMova.Application.Implementations.Servicios;
using LFMova.Application.Interfaces;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Arma un <see cref="ServicioServicio"/> real con sus colaboradores a partir de los repositorios y
/// servicios que cada prueba decide (normalmente falsos en memoria), igual que lo haría el contenedor
/// de dependencias. No contiene aserciones ni datos de prueba.
/// </summary>
internal static class ServicioServicioFabrica
{
    /// <summary>Crea el servicio conectando todos sus colaboradores con las dependencias indicadas.</summary>
    public static ServicioServicio Crear(
        IServicioRepositorio servicioRepositorio,
        IJornadaRepositorio jornadaRepositorio,
        ISedeRepositorio sedeRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        INotificacionServicio notificacionServicio,
        IFacturacionRepositorio? facturacionRepositorio = null,
        IVehiculoRepositorio? vehiculoRepositorio = null)
    {
        var acceso = new AccesoServicio(servicioRepositorio);
        var asignadorUnidad = new AsignadorUnidadServicio(
            acceso, servicioRepositorio, unidadOperativaRepositorio, conductorRepositorio, servicioPasajeroRepositorio, empleadoRepositorio, notificacionServicio);
        var modificadorRuta = new ModificadorRutaArmada(
            acceso, servicioRepositorio, unidadOperativaRepositorio, conductorRepositorio, servicioPasajeroRepositorio, notificacionServicio);
        var registradorUso = new RegistradorUsoConductor(
            facturacionRepositorio ?? new FacturacionRepositorioEnMemoria(), unidadOperativaRepositorio, conductorRepositorio,
            vehiculoRepositorio ?? new VehiculoRepositorioVacio());
        var ejecucion = new EjecucionServicio(acceso, servicioRepositorio, servicioPasajeroRepositorio, registradorUso);

        return new ServicioServicio(
            servicioRepositorio, jornadaRepositorio, sedeRepositorio, unidadOperativaRepositorio, conductorRepositorio,
            acceso, asignadorUnidad, modificadorRuta, ejecucion);
    }
}
