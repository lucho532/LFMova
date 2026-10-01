using TransportApp.Domain.Enums;

namespace TransportApp.Application.DTOs.Incidencias;

/// <summary>Datos necesarios para asociar una evidencia a una incidencia.</summary>
public class AgregarEvidenciaDto
{
    /// <summary>Tipo de evidencia.</summary>
    public TipoEvidencia Tipo { get; set; }

    /// <summary>Referencia al archivo almacenado externamente.</summary>
    public string ReferenciaArchivo { get; set; } = string.Empty;
}
