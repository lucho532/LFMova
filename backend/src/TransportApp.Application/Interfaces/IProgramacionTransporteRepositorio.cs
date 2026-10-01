using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="ProgramacionTransporte"/> para la capa de aplicación. No
/// decide reglas de negocio ni de autorización.
/// </summary>
public interface IProgramacionTransporteRepositorio
{
    /// <summary>Obtiene una programación por su identificador, o <c>null</c> si no existe.</summary>
    Task<ProgramacionTransporte?> ObtenerPorIdAsync(int programacionTransporteId);

    /// <summary>Obtiene las programaciones de la empresa indicada.</summary>
    Task<List<ProgramacionTransporte>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>
    /// Obtiene la programación que identifica de forma única a un mismo viaje (mismo empleado, sede,
    /// fecha, hora y tipo), o <c>null</c> si no existe. Se usa para recuperarse de una carrera entre
    /// importaciones concurrentes del mismo Excel.
    /// </summary>
    Task<ProgramacionTransporte?> ObtenerPorClaveAsync(int empleadoId, int sedeId, DateOnly fecha, TimeOnly hora, TipoServicio tipo);

    /// <summary>Agrega una nueva programación.</summary>
    Task AgregarAsync(ProgramacionTransporte programacion);

    /// <summary>
    /// Descarta una programación que se había agregado pero cuyo <see cref="GuardarCambiosAsync"/> falló
    /// (por ejemplo, por una carrera de importaciones concurrentes que chocó con la clave única del
    /// viaje): sin esto, el contexto lo seguiría reintentando en cada guardado posterior y fallaría
    /// siempre con el mismo error.
    /// </summary>
    void Descartar(ProgramacionTransporte programacion);

    /// <summary>Elimina por completo la programación indicada (borrado físico). Solo es posible si no está asignada a ningún <c>ServicioPasajero</c>.</summary>
    Task EliminarAsync(ProgramacionTransporte programacion);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
