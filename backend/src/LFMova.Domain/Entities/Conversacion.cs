namespace LFMova.Domain.Entities;

/// <summary>
/// Representa una conversación privada entre conductor y empleado,
/// relacionada con un <c>ServicioPasajero</c> concreto. Un
/// <c>ServicioPasajero</c> puede tener como máximo una conversación
/// (restricción única sobre <c>ServicioPasajeroId</c>, a definir a nivel de
/// base de datos). No implementa chat grupal. No contiene los mensajes en sí
/// (ver <c>Mensaje</c>) ni decide quién puede participar.
/// </summary>
public class Conversacion
{
    /// <summary>Identificador único de la conversación.</summary>
    public int ConversacionId { get; set; }

    /// <summary>Servicio pasajero al que pertenece esta conversación.</summary>
    public int ServicioPasajeroId { get; set; }

    /// <summary>Servicio pasajero al que pertenece esta conversación.</summary>
    public ServicioPasajero? ServicioPasajero { get; set; }

    /// <summary>Mensajes que forman parte de esta conversación.</summary>
    public ICollection<Mensaje> Mensajes { get; set; } = new List<Mensaje>();
}
