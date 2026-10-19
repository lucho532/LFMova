namespace LFMova.Application.DTOs.Chat;

/// <summary>Representación pública de un mensaje de la conversación.</summary>
public class MensajeDto
{
    /// <summary>Identificador único del mensaje.</summary>
    public int MensajeId { get; set; }

    /// <summary>Usuario que envió el mensaje.</summary>
    public int UsuarioId { get; set; }

    /// <summary>Contenido del mensaje.</summary>
    public string Contenido { get; set; } = string.Empty;

    /// <summary>Fecha y hora de envío del mensaje.</summary>
    public DateTime FechaHora { get; set; }
}
