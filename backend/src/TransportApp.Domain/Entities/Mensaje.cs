namespace TransportApp.Domain.Entities;

/// <summary>
/// Representa un mensaje individual dentro de una <c>Conversacion</c>. El
/// remitente se identifica mediante <c>UsuarioId</c>. No almacena
/// grabaciones de llamadas ni decide reglas de autorización sobre quién
/// puede enviar mensajes en la conversación.
/// </summary>
public class Mensaje
{
    /// <summary>Identificador único del mensaje.</summary>
    public int MensajeId { get; set; }

    /// <summary>Conversación a la que pertenece el mensaje.</summary>
    public int ConversacionId { get; set; }

    /// <summary>Usuario que envió el mensaje.</summary>
    public int UsuarioId { get; set; }

    /// <summary>Contenido del mensaje.</summary>
    public string Contenido { get; set; } = string.Empty;

    /// <summary>Fecha y hora de envío del mensaje.</summary>
    public DateTime FechaHora { get; set; }

    /// <summary>Conversación a la que pertenece el mensaje.</summary>
    public Conversacion? Conversacion { get; set; }

    /// <summary>Usuario que envió el mensaje.</summary>
    public Usuario? Usuario { get; set; }
}
