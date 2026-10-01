namespace TransportApp.Application.DTOs.Invitaciones;

/// <summary>Token del enlace de invitación que la persona autenticada acepta.</summary>
public class AceptarInvitacionDto
{
    /// <summary>Token recibido en el enlace del correo.</summary>
    public string Token { get; set; } = string.Empty;
}
