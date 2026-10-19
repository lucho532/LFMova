namespace LFMova.Application.DTOs.Notificaciones;

/// <summary>Representa una notificación tal como se expone al usuario destinatario.</summary>
public class NotificacionDto
{
    /// <summary>Identificador único de la notificación.</summary>
    public int NotificacionId { get; set; }

    /// <summary>Tipo/categoría descriptiva de la notificación.</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Título de la notificación.</summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Contenido del mensaje de la notificación.</summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Indica si la notificación fue leída por el usuario.</summary>
    /// <summary>Ruta de la aplicación a la que lleva la notificación, si corresponde.</summary>
    public string? Enlace { get; set; }

    public bool Leida { get; set; }

    /// <summary>Fecha y hora en que se generó la notificación.</summary>
    public DateTime FechaHora { get; set; }
}
