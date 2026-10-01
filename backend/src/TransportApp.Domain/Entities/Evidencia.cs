using TransportApp.Domain.Enums;

namespace TransportApp.Domain.Entities;

/// <summary>
/// Representa una evidencia asociada a una <c>Incidencia</c>. Almacena
/// únicamente una referencia al archivo, no el archivo binario dentro de
/// PostgreSQL. El almacenamiento externo concreto se define técnicamente en
/// una tarea posterior.
/// </summary>
public class Evidencia
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

    /// <summary>Incidencia a la que pertenece esta evidencia.</summary>
    public Incidencia? Incidencia { get; set; }
}
