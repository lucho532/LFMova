using LFMova.Application.DTOs.Soportes;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;

namespace LFMova.Application.Implementations.Jornadas;

/// <summary>
/// Arma el bloque de una ruta para un soporte en Excel: sus datos y sus
/// pasajeros en orden de recogida. Solo lee: no decide qué rutas van en cada
/// soporte ni a quién se le entrega.
/// </summary>
public class ArmadorSoporteRutas
{
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;

    /// <summary>Crea el colaborador con sus repositorios.</summary>
    public ArmadorSoporteRutas(IServicioPasajeroRepositorio servicioPasajeroRepositorio, IEmpleadoRepositorio empleadoRepositorio)
    {
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
    }

    /// <summary>
    /// Arma la ruta con sus pasajeros. <paramref name="nombreConductor"/> se indica solo cuando el
    /// soporte reúne rutas de varios conductores y hay que decir de quién es cada una.
    /// </summary>
    public async Task<RutaSoporte> ArmarAsync(Servicio ruta, string? nombreConductor = null)
    {
        var rutaSoporte = new RutaSoporte
        {
            Tipo = ruta.Tipo,
            NombreSede = ruta.Sede?.Nombre ?? string.Empty,
            Fecha = ruta.Fecha,
            Hora = ruta.HoraProgramada,
            NombreConductor = nombreConductor
        };

        var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(ruta.ServicioId);
        foreach (var pasajero in pasajeros.OrderBy(p => p.Orden))
        {
            var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(pasajero.EmpleadoId);
            rutaSoporte.Pasajeros.Add(new PasajeroSoporte
            {
                Orden = pasajero.Orden,
                Cedula = empleado?.Usuario?.Cedula ?? string.Empty,
                NombreCompleto = empleado?.NombreCompleto ?? string.Empty,
                Telefono = empleado?.Telefono ?? string.Empty,
                // La dirección es la que quedó guardada para ese servicio, no la actual del empleado.
                Direccion = pasajero.DireccionRecogida,
                Barrio = empleado?.Barrio ?? string.Empty
            });
        }

        return rutaSoporte;
    }
}
