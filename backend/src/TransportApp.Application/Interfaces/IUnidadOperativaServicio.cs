using TransportApp.Application.DTOs.UnidadesOperativas;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define los casos de uso de creación y gestión de unidades operativas
/// (conductor + vehículo). No decide autorización: eso se verifica en la capa
/// de Api antes de invocar estos métodos.
/// </summary>
public interface IUnidadOperativaServicio
{
    /// <summary>
    /// Crea una unidad operativa a partir del conductor y el vehículo
    /// indicados. Falla si el vehículo no pertenece a ese conductor o si ya
    /// tiene una unidad operativa registrada.
    /// </summary>
    Task<UnidadOperativaDto> CrearAsync(int conductorId, CrearUnidadOperativaDto datos);

    /// <summary>Obtiene las unidades operativas del conductor indicado.</summary>
    Task<List<UnidadOperativaDto>> ObtenerPorConductorAsync(int conductorId);

    /// <summary>Activa una unidad operativa del conductor indicado.</summary>
    Task ActivarAsync(int conductorId, int unidadOperativaId);

    /// <summary>Desactiva una unidad operativa del conductor indicado.</summary>
    Task DesactivarAsync(int conductorId, int unidadOperativaId);
}
