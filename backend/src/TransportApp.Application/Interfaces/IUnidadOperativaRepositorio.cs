using TransportApp.Domain.Entities;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="UnidadOperativa"/> para la capa de aplicación. No decide reglas
/// de negocio ni de autorización.
/// </summary>
public interface IUnidadOperativaRepositorio
{
    /// <summary>Obtiene una unidad operativa por su identificador, o <c>null</c> si no existe.</summary>
    Task<UnidadOperativa?> ObtenerPorIdAsync(int unidadOperativaId);

    /// <summary>
    /// Obtiene la unidad operativa registrada para el vehículo indicado, o
    /// <c>null</c> si el vehículo no tiene ninguna.
    /// </summary>
    Task<UnidadOperativa?> ObtenerPorVehiculoAsync(int vehiculoId);

    /// <summary>Obtiene las unidades operativas del conductor indicado.</summary>
    Task<List<UnidadOperativa>> ObtenerPorConductorAsync(int conductorId);

    /// <summary>Agrega una nueva unidad operativa.</summary>
    Task AgregarAsync(UnidadOperativa unidadOperativa);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
