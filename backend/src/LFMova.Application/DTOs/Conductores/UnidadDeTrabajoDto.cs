using LFMova.Application.DTOs.Vehiculos;

namespace LFMova.Application.DTOs.Conductores;

/// <summary>
/// Unidad operativa de un conductor junto con los datos completos de su
/// vehículo, para que el propio conductor vea con qué trabaja.
/// </summary>
public class UnidadDeTrabajoDto
{
    /// <summary>Identificador de la unidad operativa.</summary>
    public int UnidadOperativaId { get; set; }

    /// <summary>Indica si la unidad operativa está activa.</summary>
    public bool Activa { get; set; }

    /// <summary>Vehículo de la unidad.</summary>
    public VehiculoDto Vehiculo { get; set; } = new();
}
