using LFMova.Domain.Entities;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre <see cref="Vehiculo"/>
/// para la capa de aplicación. No decide reglas de negocio ni de autorización.
/// </summary>
public interface IVehiculoRepositorio
{
    /// <summary>Obtiene un vehículo por su identificador, o <c>null</c> si no existe.</summary>
    Task<Vehiculo?> ObtenerPorIdAsync(int vehiculoId);

    /// <summary>Obtiene los vehículos del conductor indicado.</summary>
    Task<List<Vehiculo>> ObtenerPorConductorAsync(int conductorId);

    /// <summary>Obtiene el vehículo con la placa indicada, o <c>null</c> si no existe ninguno.</summary>
    Task<Vehiculo?> ObtenerPorPlacaAsync(string placa);

    /// <summary>Agrega un nuevo vehículo.</summary>
    Task AgregarAsync(Vehiculo vehiculo);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
