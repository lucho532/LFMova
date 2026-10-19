namespace LFMova.Domain.Enums;

/// <summary>
/// Representa el estado actual de un <c>Servicio</c> dentro de su ciclo
/// operativo. Solamente se conserva el estado actual; esta versión no
/// implementa historial automático de estados.
/// </summary>
public enum EstadoServicio
{
    /// <summary>Servicio creado pero todavía no organizado para su asignación.</summary>
    BORRADOR,

    /// <summary>Servicio organizado, pendiente de asignar una unidad operativa.</summary>
    PENDIENTE_ASIGNACION,

    /// <summary>Servicio con una unidad operativa asignada.</summary>
    ASIGNADO,

    /// <summary>Servicio comunicado al conductor y a los empleados involucrados.</summary>
    PUBLICADO,

    /// <summary>Servicio en ejecución por el conductor.</summary>
    EN_CURSO,

    /// <summary>Servicio finalizado.</summary>
    FINALIZADO,

    /// <summary>Servicio cancelado.</summary>
    CANCELADO
}
