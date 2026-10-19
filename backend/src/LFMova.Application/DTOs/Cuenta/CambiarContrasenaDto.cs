namespace LFMova.Application.DTOs.Cuenta;

/// <summary>Datos para que el usuario autenticado cambie su propia contraseña.</summary>
public class CambiarContrasenaDto
{
    /// <summary>Contraseña actual, para comprobar que quien pide el cambio es la persona titular.</summary>
    public string ContrasenaActual { get; set; } = string.Empty;

    /// <summary>Nueva contraseña en texto plano.</summary>
    public string NuevaContrasena { get; set; } = string.Empty;

    /// <summary>Confirmación de la nueva contraseña. Debe coincidir con <see cref="NuevaContrasena"/>.</summary>
    public string ConfirmacionContrasena { get; set; } = string.Empty;
}
