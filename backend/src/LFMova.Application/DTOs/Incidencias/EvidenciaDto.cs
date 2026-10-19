using LFMova.Domain.Enums;

namespace LFMova.Application.DTOs.Incidencias;

/// <summary>Representación pública de una evidencia asociada a una incidencia.</summary>
public class EvidenciaDto
{
    /// <summary>Identificador único de la evidencia.</summary>
    public int EvidenciaId { get; set; }

    /// <summary>Incidencia a la que pertenece esta evidencia.</summary>
    public int IncidenciaId { get; set; }

    /// <summary>Tipo de evidencia.</summary>
    public TipoEvidencia Tipo { get; set; }

    /// <summary>Referencia al archivo almacenado externamente.</summary>
    public string ReferenciaArchivo { get; set; } = string.Empty;

    /// <summary>Fecha y hora en que se registró la evidencia.</summary>
    public DateTime FechaHora { get; set; }
}
