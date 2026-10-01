using TransportApp.Domain.Entities;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="Incidencia"/> para la capa de aplicación. No decide reglas de
/// negocio ni de autorización.
/// </summary>
public interface IIncidenciaRepositorio
{
    /// <summary>Obtiene una incidencia por su identificador, o <c>null</c> si no existe.</summary>
    Task<Incidencia?> ObtenerPorIdAsync(int incidenciaId);

    /// <summary>Obtiene las incidencias del pasajero indicado.</summary>
    Task<List<Incidencia>> ObtenerPorServicioPasajeroAsync(int servicioPasajeroId);

    /// <summary>Obtiene las incidencias de todos los pasajeros del servicio indicado, para el resumen de la ruta del coordinador.</summary>
    Task<List<Incidencia>> ObtenerPorServicioIdAsync(int servicioId);

    /// <summary>Agrega una nueva incidencia.</summary>
    Task AgregarAsync(Incidencia incidencia);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
