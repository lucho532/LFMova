namespace TransportApp.Application.DTOs.Autenticacion;

/// <summary>
/// Resultado de un inicio de sesión exitoso: el token JWT emitido y su
/// momento de expiración.
/// </summary>
public class RespuestaAutenticacionDto
{
    /// <summary>Token JWT emitido para el usuario autenticado.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Momento (UTC) en que el token expira.</summary>
    public DateTime ExpiraEnUtc { get; set; }
}
