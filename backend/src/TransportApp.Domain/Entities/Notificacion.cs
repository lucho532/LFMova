namespace TransportApp.Domain.Entities;

/// <summary>
/// Representa una notificación dirigida a un <c>Usuario</c>. Un usuario
/// puede recibir múltiples notificaciones. No decide a quién debe
/// notificarse ni cuándo generar una notificación; esas reglas corresponden
/// a la capa de aplicación.
/// </summary>
public class Notificacion
{
    /// <summary>Identificador único de la notificación.</summary>
    public int NotificacionId { get; set; }

    /// <summary>Usuario destinatario de la notificación.</summary>
    public int UsuarioId { get; set; }

    /// <summary>
    /// Tipo/categoría descriptiva de la notificación (por ejemplo,
    /// publicación de servicio, cambio de conductor, incidencia relevante).
    /// No corresponde a un catálogo cerrado definido en el SDD.
    /// </summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Título de la notificación.</summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Contenido del mensaje de la notificación.</summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Indica si la notificación fue leída por el usuario.</summary>
    /// <summary>Ruta de la aplicación a la que lleva la notificación al abrirla (por ejemplo, el chat del pasajero), o <c>null</c> si no lleva a ningún lado.</summary>
    public string? Enlace { get; set; }

    public bool Leida { get; set; }

    /// <summary>Fecha y hora en que se generó la notificación.</summary>
    public DateTime FechaHora { get; set; }

    /// <summary>Usuario destinatario de la notificación.</summary>
    public Usuario? Usuario { get; set; }
}
