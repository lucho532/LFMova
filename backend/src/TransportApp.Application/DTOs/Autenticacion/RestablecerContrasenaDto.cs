namespace TransportApp.Application.DTOs.Autenticacion;

/// <summary>
/// Datos para establecer la contraseña de una cuenta mediante el token
/// enviado por correo. Se usa tanto para la recuperación de contraseña como
/// para que una persona invitada (por ejemplo, el primer coordinador de una
/// empresa) establezca su contraseña inicial.
/// </summary>
public class RestablecerContrasenaDto
{
    /// <summary>Token recibido por correo, en texto plano.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Nueva contraseña en texto plano proporcionada por el usuario.</summary>
    public string NuevaContrasena { get; set; } = string.Empty;

    /// <summary>Confirmación de la nueva contraseña. Debe coincidir con <see cref="NuevaContrasena"/>.</summary>
    public string ConfirmacionContrasena { get; set; } = string.Empty;
}
