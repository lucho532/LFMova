using LFMova.Application.Implementations;
using LFMova.Application.Implementations.Jornadas;
using LFMova.Application.Interfaces;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Arma un <see cref="JornadaServicio"/> real con su colaborador a partir de los repositorios y
/// servicios que cada prueba decide (normalmente falsos en memoria), igual que lo haría el contenedor
/// de dependencias. No contiene aserciones ni datos de prueba.
/// </summary>
internal static class JornadaServicioFabrica
{
    /// <summary>Crea el servicio conectando su colaborador con las dependencias indicadas.</summary>
    public static JornadaServicio Crear(
        IJornadaRepositorio jornadaRepositorio,
        IServicioRepositorio servicioRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IProgramacionTransporteRepositorio programacionRepositorio,
        INotificacionServicio notificacionServicio)
        => new(
            jornadaRepositorio, servicioRepositorio, unidadOperativaRepositorio, conductorRepositorio, servicioPasajeroRepositorio,
            empleadoRepositorio, notificacionServicio,
            new DepuradorJornada(jornadaRepositorio, servicioRepositorio, servicioPasajeroRepositorio, programacionRepositorio));
}
