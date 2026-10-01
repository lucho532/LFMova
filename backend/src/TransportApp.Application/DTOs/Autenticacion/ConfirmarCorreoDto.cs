namespace TransportApp.Application.DTOs.Autenticacion;

/// <summary>Datos para confirmar el correo de una cuenta mediante el token enviado por correo.</summary>
public class ConfirmarCorreoDto
{
    /// <summary>Token de confirmación recibido por correo, en texto plano.</summary>
    public string Token { get; set; } = string.Empty;
}
