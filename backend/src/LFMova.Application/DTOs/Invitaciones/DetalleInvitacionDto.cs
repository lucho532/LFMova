namespace LFMova.Application.DTOs.Invitaciones;

/// <summary>
/// Lo que ve la persona invitada al abrir el enlace del correo: quién la
/// invita, a qué empresa y si necesita crear su cuenta primero. Solo lo
/// obtiene quien tiene el enlace (enviado a su propio correo).
/// </summary>
public class DetalleInvitacionDto
{
    /// <summary>Nombre de la empresa que invita.</summary>
    public string NombreEmpresa { get; set; } = string.Empty;

    /// <summary>Nombre del coordinador que envió la invitación.</summary>
    public string NombreInvitador { get; set; } = string.Empty;

    /// <summary>Cédula invitada (para completar el registro).</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Correo al que llegó la invitación (para completar el registro).</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Indica que la persona todavía no tiene una cuenta activa y debe crearla desde el enlace.</summary>
    public bool RequiereRegistro { get; set; }

    /// <summary>PENDIENTE, ACEPTADA o VENCIDA.</summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>Momento en que vence la invitación.</summary>
    public DateTime FechaExpiracion { get; set; }
}
