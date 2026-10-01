namespace LFMova.Application.DTOs.Invitaciones;

/// <summary>
/// Invitación enviada por la empresa, tal como la ve un coordinador. Nunca
/// incluye el correo de destino ni otros datos de la persona antes de que la
/// acepte: solo la cédula que escribió el propio coordinador.
/// </summary>
public class InvitacionEmpresaDto
{
    /// <summary>Identificador de la invitación.</summary>
    public int InvitacionEmpresaId { get; set; }

    /// <summary>Cédula invitada (la que escribió el coordinador).</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre de la persona, solo cuando ya aceptó; <c>null</c> mientras tanto.</summary>
    public string? NombreCompleto { get; set; }

    /// <summary>PENDIENTE, ACEPTADA o VENCIDA.</summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>Momento en que se envió la invitación.</summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>Momento en que vence (o venció) la invitación.</summary>
    public DateTime FechaExpiracion { get; set; }

    /// <summary>Momento en que la persona la aceptó, o <c>null</c>.</summary>
    public DateTime? FechaAceptacion { get; set; }
}
