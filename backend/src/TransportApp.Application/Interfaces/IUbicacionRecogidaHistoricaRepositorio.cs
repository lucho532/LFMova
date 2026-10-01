using TransportApp.Domain.Entities;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="UbicacionRecogidaHistorica"/> para la capa de aplicación. No
/// decide reglas de negocio ni de autorización.
/// </summary>
public interface IUbicacionRecogidaHistoricaRepositorio
{
    /// <summary>Agrega una nueva ubicación histórica de recogida.</summary>
    Task AgregarAsync(UbicacionRecogidaHistorica ubicacion);

    /// <summary>Obtiene las ubicaciones históricas de un empleado, de la más reciente a la más antigua.</summary>
    Task<List<UbicacionRecogidaHistorica>> ObtenerPorEmpleadoAsync(int empleadoId);

    /// <summary>Elimina todas las ubicaciones históricas guardadas de un empleado (para volver a guardar una nueva desde cero).</summary>
    Task EliminarPorEmpleadoAsync(int empleadoId);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
