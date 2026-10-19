using LFMova.Domain.Entities;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="ServicioPasajero"/> para la capa de aplicación. No decide
/// reglas de negocio ni de autorización.
/// </summary>
public interface IServicioPasajeroRepositorio
{
    /// <summary>Obtiene un pasajero de servicio por su identificador, o <c>null</c> si no existe.</summary>
    Task<ServicioPasajero?> ObtenerPorIdAsync(int servicioPasajeroId);

    /// <summary>
    /// Obtiene el <see cref="ServicioPasajero"/> creado a partir de la
    /// programación indicada, o <c>null</c> si esa programación todavía no
    /// ha sido asignada a ningún servicio.
    /// </summary>
    Task<ServicioPasajero?> ObtenerPorProgramacionAsync(int programacionTransporteId);

    /// <summary>Obtiene los pasajeros del servicio indicado.</summary>
    Task<List<ServicioPasajero>> ObtenerPorServicioAsync(int servicioId);

    /// <summary>
    /// Obtiene las participaciones del empleado indicado en cualquier
    /// servicio (de cualquier empresa, pasadas y futuras), incluyendo el
    /// <see cref="Servicio"/> y su <see cref="Jornada"/> para poder resolver
    /// la empresa, fecha, hora y estado de cada una.
    /// </summary>
    Task<List<ServicioPasajero>> ObtenerPorEmpleadoAsync(int empleadoId);

    /// <summary>Agrega un nuevo pasajero de servicio.</summary>
    Task AgregarAsync(ServicioPasajero servicioPasajero);

    /// <summary>
    /// Descarta un pasajero de servicio que se había agregado pero cuyo <see cref="GuardarCambiosAsync"/>
    /// falló (por ejemplo, por una carrera de importaciones concurrentes que chocó con la programación
    /// única): sin esto, el contexto lo seguiría reintentando en cada guardado posterior y fallaría siempre
    /// con el mismo error.
    /// </summary>
    void Descartar(ServicioPasajero servicioPasajero);

    /// <summary>Elimina por completo el pasajero indicado (borrado físico, no un cambio de estado).</summary>
    Task EliminarAsync(ServicioPasajero servicioPasajero);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
