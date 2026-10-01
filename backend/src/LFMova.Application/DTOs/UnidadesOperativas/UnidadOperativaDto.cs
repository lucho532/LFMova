namespace LFMova.Application.DTOs.UnidadesOperativas;

/// <summary>Representación pública de una unidad operativa (conductor + vehículo).</summary>
public class UnidadOperativaDto
{
    /// <summary>Identificador único de la unidad operativa.</summary>
    public int UnidadOperativaId { get; set; }

    /// <summary>Identificador del conductor de la unidad.</summary>
    public int ConductorId { get; set; }

    /// <summary>Identificador del vehículo de la unidad.</summary>
    public int VehiculoId { get; set; }

    /// <summary>Indica si la unidad operativa está activa.</summary>
    public bool Activa { get; set; }
}
