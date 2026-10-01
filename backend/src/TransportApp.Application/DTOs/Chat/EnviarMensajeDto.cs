namespace TransportApp.Application.DTOs.Chat;

/// <summary>Datos necesarios para enviar un mensaje en la conversación.</summary>
public class EnviarMensajeDto
{
    /// <summary>Contenido del mensaje.</summary>
    public string Contenido { get; set; } = string.Empty;
}
