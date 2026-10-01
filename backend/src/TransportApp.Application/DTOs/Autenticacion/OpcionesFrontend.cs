namespace TransportApp.Application.DTOs.Autenticacion;

/// <summary>
/// Configuración externa con la URL base del frontend (sección de
/// configuración <c>"Frontend"</c>), usada para construir los enlaces que se
/// envían por correo (confirmación de cuenta, restablecimiento de
/// contraseña). Nunca debe contener valores reales hardcodeados en el código
/// fuente.
/// </summary>
public class OpcionesFrontend
{
    /// <summary>Nombre de la sección de configuración correspondiente.</summary>
    public const string Seccion = "Frontend";

    /// <summary>URL base del frontend, sin barra final.</summary>
    public string UrlBase { get; set; } = string.Empty;
}
