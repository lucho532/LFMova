namespace LFMova.Infrastructure.Correo;

/// <summary>
/// Representa la configuración externa necesaria para enviar correos
/// mediante la API de Brevo (sección de configuración <c>"Brevo"</c>). Nunca
/// debe contener valores reales hardcodeados en el código fuente.
/// </summary>
public class OpcionesBrevo
{
    /// <summary>Nombre de la sección de configuración correspondiente.</summary>
    public const string Seccion = "Brevo";

    /// <summary>Clave de API de Brevo.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Correo remitente autorizado en la cuenta de Brevo.</summary>
    public string RemitenteEmail { get; set; } = string.Empty;

    /// <summary>Nombre del remitente mostrado al destinatario.</summary>
    public string RemitenteNombre { get; set; } = string.Empty;
}
