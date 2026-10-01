namespace TransportApp.Domain.Enums;

/// <summary>
/// Representa el tipo de una <c>Evidencia</c> asociada a una <c>Incidencia</c>.
/// La plataforma admite inicialmente un único tipo de evidencia. Cualquier
/// tipo adicional (documentos u otros archivos) requiere una decisión
/// explícita antes de implementarse; no debe inventarse.
/// </summary>
public enum TipoEvidencia
{
    /// <summary>Fotografía tomada por el conductor como evidencia de la incidencia.</summary>
    FOTOGRAFIA
}
