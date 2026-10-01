namespace LFMova.Domain.Enums;

/// <summary>
/// Representa el estado actual de un <c>ServicioPasajero</c> durante la
/// ejecución de un servicio. Solamente se conserva el estado actual; esta
/// versión no implementa historial automático de cambios de estado. El motivo
/// por el que no se recogió a un pasajero (no contesta, no se encuentra,
/// dirección incorrecta, no se pudo recoger) no es un estado: queda en la
/// incidencia que registra el conductor.
/// </summary>
public enum EstadoServicioPasajero
{
    /// <summary>Pasajero programado para el servicio.</summary>
    PROGRAMADO,

    /// <summary>El pasajero confirmó su asistencia.</summary>
    CONFIRMADO,

    /// <summary>El pasajero indicó que no asistirá.</summary>
    NO_ASISTIRA,

    /// <summary>El conductor llegó al punto de recogida del pasajero.</summary>
    CONDUCTOR_LLEGO,

    /// <summary>El pasajero fue recogido (se considera transportado al finalizar el servicio).</summary>
    RECOGIDO,

    /// <summary>No se recogió al pasajero; el motivo está en la incidencia registrada.</summary>
    NO_RECOGIDO,

    /// <summary>La participación del pasajero en el servicio fue cancelada.</summary>
    CANCELADO
}
