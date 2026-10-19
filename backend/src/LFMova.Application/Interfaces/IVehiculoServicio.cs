using LFMova.Application.DTOs.Vehiculos;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define los casos de uso de registro y gestión de vehículos de un
/// conductor. No decide autorización: eso se verifica en la capa de Api
/// antes de invocar estos métodos.
/// </summary>
public interface IVehiculoServicio
{
    /// <summary>Registra un nuevo vehículo perteneciente al conductor indicado.</summary>
    Task<VehiculoDto> CrearAsync(int conductorId, CrearVehiculoDto datos);

    /// <summary>Obtiene un vehículo por su identificador, o <c>null</c> si no existe.</summary>
    Task<VehiculoDto?> ObtenerPorIdAsync(int vehiculoId);

    /// <summary>
    /// Actualiza los datos de un vehículo del conductor (placa, marca, modelo,
    /// capacidad y vigencias de SOAT y técnico-mecánica) con las mismas
    /// validaciones que el alta. Falla si el vehículo no es del conductor o si
    /// la nueva placa ya pertenece a otro vehículo.
    /// </summary>
    Task<VehiculoDto> ActualizarAsync(int conductorId, int vehiculoId, CrearVehiculoDto datos);

    /// <summary>Obtiene los vehículos del conductor indicado.</summary>
    Task<List<VehiculoDto>> ObtenerPorConductorAsync(int conductorId);

    /// <summary>Activa un vehículo del conductor indicado.</summary>
    Task ActivarAsync(int conductorId, int vehiculoId);

    /// <summary>
    /// Desactiva un vehículo del conductor indicado. Si el vehículo tiene una
    /// unidad operativa activa asociada, también se desactiva.
    /// </summary>
    Task DesactivarAsync(int conductorId, int vehiculoId);
}
