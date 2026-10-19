namespace LFMova.Domain.Enums;

/// <summary>
/// Representa el estado actual de una <c>ImportacionExcel</c> ejecutada por un
/// coordinador. Solamente se conserva el estado actual de la importación.
/// </summary>
public enum EstadoImportacionExcel
{
    /// <summary>La importación fue registrada pero todavía no se ha procesado.</summary>
    PENDIENTE,

    /// <summary>La importación se está procesando.</summary>
    PROCESANDO,

    /// <summary>La importación finalizó sin incidencias.</summary>
    COMPLETADA,

    /// <summary>La importación finalizó, pero se detectaron situaciones que requieren revisión del coordinador.</summary>
    COMPLETADA_CON_ADVERTENCIAS,

    /// <summary>La importación no pudo completarse debido a un error.</summary>
    ERROR
}
