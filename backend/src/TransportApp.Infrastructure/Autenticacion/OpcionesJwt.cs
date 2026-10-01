namespace TransportApp.Infrastructure.Autenticacion;

/// <summary>
/// Representa la configuración externa necesaria para firmar y validar
/// tokens JWT (sección de configuración <c>"Jwt"</c>). Nunca debe contener
/// valores reales hardcodeados en el código fuente.
/// </summary>
public class OpcionesJwt
{
    /// <summary>Nombre de la sección de configuración correspondiente.</summary>
    public const string Seccion = "Jwt";

    /// <summary>Clave secreta utilizada para firmar los tokens.</summary>
    public string ClaveSecreta { get; set; } = string.Empty;

    /// <summary>Emisor del token.</summary>
    public string Emisor { get; set; } = string.Empty;

    /// <summary>Audiencia del token.</summary>
    public string Audiencia { get; set; } = string.Empty;

    /// <summary>Minutos de vigencia del token desde su emisión.</summary>
    public int ExpiracionMinutos { get; set; } = 60;
}
