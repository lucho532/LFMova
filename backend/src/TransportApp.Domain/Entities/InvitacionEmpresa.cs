namespace TransportApp.Domain.Entities;

/// <summary>
/// Representa la invitación que un coordinador envía por correo a una
/// persona (identificada por su cédula) para que se una a su empresa. Es la
/// única forma en que un coordinador llega a ver los datos de alguien que no
/// es todavía parte de su empresa: solo después de que la persona acepta
/// (desde el enlace del correo) queda como empleado de la empresa y el
/// coordinador puede asignarle un rol. El token del enlace nunca se almacena
/// en texto plano. Su estado (pendiente, aceptada o vencida) se deriva de las
/// fechas; no guarda historial. No decide su propia validez: eso corresponde
/// a la capa de dominio/aplicación.
/// </summary>
public class InvitacionEmpresa
{
    /// <summary>Identificador único de la invitación.</summary>
    public int InvitacionEmpresaId { get; set; }

    /// <summary>Empresa a la que se invita a la persona.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Cédula de la persona invitada: solo la cuenta con esta cédula puede aceptarla.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>
    /// Correo al que se envió la invitación. Si la persona ya tenía cuenta con
    /// correo, es el de su cuenta (nunca el que escribió el coordinador), para
    /// que la invitación llegue siempre a su verdadero dueño.
    /// </summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Usuario (coordinador) que envió la invitación.</summary>
    public int UsuarioInvitadorId { get; set; }

    /// <summary>Hash del token del enlace. El token en texto plano nunca se persiste.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Momento en que se creó la invitación.</summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>Momento a partir del cual la invitación deja de poder aceptarse.</summary>
    public DateTime FechaExpiracion { get; set; }

    /// <summary>Momento en que la persona aceptó la invitación, o <c>null</c> si todavía no la aceptó.</summary>
    public DateTime? FechaAceptacion { get; set; }

    /// <summary>Usuario que aceptó la invitación (el dueño de la cédula), o <c>null</c> si todavía no se aceptó.</summary>
    public int? UsuarioAceptanteId { get; set; }

    /// <summary>Empresa a la que se invita.</summary>
    public Empresa? Empresa { get; set; }

    /// <summary>Coordinador que envió la invitación.</summary>
    public Usuario? UsuarioInvitador { get; set; }

    /// <summary>Usuario que aceptó la invitación.</summary>
    public Usuario? UsuarioAceptante { get; set; }
}
