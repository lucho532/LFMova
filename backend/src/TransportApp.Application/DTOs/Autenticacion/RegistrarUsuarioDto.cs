namespace TransportApp.Application.DTOs.Autenticacion;

/// <summary>
/// Datos necesarios para que cualquier persona se autorregistre en la
/// plataforma. Crea la cuenta con el rol <c>EMPLEADO</c> sin empresa
/// asignada: la empresa se asigna automáticamente cuando una importación de
/// Excel encuentra su cédula (ver <c>AGENTS.md</c> §15).
/// </summary>
public class RegistrarUsuarioDto
{
    /// <summary>Correo electrónico de la persona. Debe ser único en la plataforma.</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Nombre completo de la persona.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Cédula de la persona. Debe ser única en la plataforma.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto de la persona.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Contraseña elegida por la persona.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Token del enlace de invitación a una empresa, cuando la persona se
    /// registra desde ese enlace: queda unida a esa empresa como empleado y,
    /// si usa el mismo correo al que llegó la invitación, su correo ya queda
    /// confirmado. Opcional.
    /// </summary>
    public string? TokenInvitacion { get; set; }
}
