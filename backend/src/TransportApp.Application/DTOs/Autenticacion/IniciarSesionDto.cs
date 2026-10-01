namespace TransportApp.Application.DTOs.Autenticacion;

/// <summary>
/// Datos de entrada para iniciar sesión mediante cédula o correo electrónico
/// y contraseña.
/// </summary>
public class IniciarSesionDto
{
    /// <summary>Cédula o correo electrónico del usuario.</summary>
    public string Identificador { get; set; } = string.Empty;

    /// <summary>Contraseña en texto plano proporcionada por el usuario.</summary>
    public string Password { get; set; } = string.Empty;
}
