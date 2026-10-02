using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Domain.Entities;

namespace LFMova.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="ServicioPasajero"/> y su DTO. No contiene
/// reglas de negocio ni realiza consultas de persistencia.
/// </summary>
public static class ServicioPasajeroMapper
{
    /// <summary>
    /// Convierte una entidad <see cref="ServicioPasajero"/> en su DTO
    /// público. El <paramref name="empleado"/> es opcional: cuando se
    /// proporciona, enriquece el DTO con nombre y teléfono de contacto (ver
    /// <c>tasks.md</c> T086).
    /// </summary>
    public static ServicioPasajeroDto AServicioPasajeroDto(ServicioPasajero servicioPasajero, Empleado? empleado = null) => new()
    {
        ServicioPasajeroId = servicioPasajero.ServicioPasajeroId,
        ServicioId = servicioPasajero.ServicioId,
        ProgramacionTransporteId = servicioPasajero.ProgramacionTransporteId,
        EmpleadoId = servicioPasajero.EmpleadoId,
        Estado = servicioPasajero.Estado,
        Orden = servicioPasajero.Orden,
        HoraLlegadaConductor = servicioPasajero.HoraLlegadaConductor,
        HoraProcesado = servicioPasajero.HoraProcesado,
        DireccionRecogida = servicioPasajero.DireccionRecogida,
        Latitud = servicioPasajero.Latitud,
        Longitud = servicioPasajero.Longitud,
        LatitudCompartida = servicioPasajero.LatitudCompartida,
        LongitudCompartida = servicioPasajero.LongitudCompartida,
        FechaHoraUbicacionCompartida = servicioPasajero.FechaHoraUbicacionCompartida,
        NombreCompletoEmpleado = empleado?.NombreCompleto ?? string.Empty,
        TelefonoEmpleado = empleado?.Telefono ?? string.Empty,
        CedulaEmpleado = empleado?.Usuario?.Cedula ?? string.Empty,
        BarrioEmpleado = empleado?.Barrio ?? string.Empty
    };

    /// <summary>
    /// Convierte una entidad <see cref="ServicioPasajero"/> en
    /// <see cref="ServicioDelEmpleadoDto"/>. Requiere que
    /// <see cref="ServicioPasajero.Servicio"/> y su
    /// <see cref="Servicio.Jornada"/> ya estén cargados.
    /// </summary>
    public static ServicioDelEmpleadoDto AServicioDelEmpleadoDto(ServicioPasajero servicioPasajero)
    {
        var servicio = servicioPasajero.Servicio!;
        return new ServicioDelEmpleadoDto
        {
            ServicioPasajeroId = servicioPasajero.ServicioPasajeroId,
            EmpresaId = servicio.Jornada!.EmpresaId,
            JornadaId = servicio.JornadaId,
            ServicioId = servicio.ServicioId,
            Fecha = servicio.Fecha,
            HoraProgramada = servicio.HoraProgramada,
            Tipo = servicio.Tipo,
            EstadoServicio = servicio.Estado,
            EstadoServicioPasajero = servicioPasajero.Estado,
            SedeId = servicio.SedeId,
            DireccionRecogida = servicioPasajero.DireccionRecogida,
            HoraLlegadaConductor = servicioPasajero.HoraLlegadaConductor,
            HoraProcesado = servicioPasajero.HoraProcesado,
            ConductorNombre = servicio.UnidadOperativa?.Conductor?.Usuario?.NombreCompleto,
            Placa = servicio.UnidadOperativa?.Vehiculo?.Placa,
            HoraInicioReal = servicio.HoraInicioReal,
            HoraFinReal = servicio.HoraFinReal
        };
    }
}
