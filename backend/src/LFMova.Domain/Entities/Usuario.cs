namespace LFMova.Domain.Entities;

/// <summary>
/// Representa la identidad autenticable de una persona dentro de la plataforma.
/// Su responsabilidad es identificar de forma única a la persona mediante su
/// cédula y almacenar el estado de su credencial de acceso.
/// No contiene el rol de la persona (ver <c>UsuarioRol</c>) ni su empresa: la
/// empresa se resuelve mediante <c>UsuarioRol</c>, <c>Empleado</c> o
/// <c>VinculacionConductorEmpresa</c> según corresponda. No debe contener
/// lógica de autenticación, autorización ni reglas de negocio.
/// </summary>
public class Usuario
{
    /// <summary>Identificador único interno del usuario.</summary>
    public int UsuarioId { get; set; }

    /// <summary>
    /// Identificador único global de la persona. No cambia al cambiar de
    /// empresa y no debe utilizarse como sustituto de las claves primarias
    /// internas.
    /// </summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>
    /// Correo electrónico de la persona. Único cuando está presente (una
    /// misma dirección no puede pertenecer a dos usuarios). Es el canal
    /// utilizado para confirmar la identidad de la cuenta y para recuperar la
    /// contraseña; reemplaza al mecanismo de código generado por un
    /// conductor.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Nombre completo proporcionado por la propia persona al registrarse o
    /// al ser invitada. Es independiente del nombre que pueda existir en
    /// <c>Empleado</c> o <c>Conductor</c> una vez se les asigne ese perfil:
    /// esos perfiles conservan su propio dato operativo (por ejemplo, el que
    /// traiga una importación de Excel).
    /// </summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto proporcionado por la propia persona.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>
    /// Indica si el correo de la cuenta ya fue confirmado (mediante el enlace
    /// enviado a <see cref="Email"/>). Una cuenta no puede iniciar sesión
    /// hasta que su correo esté confirmado.
    /// </summary>
    public bool CorreoConfirmado { get; set; }

    /// <summary>
    /// Hash de la contraseña del usuario. Es <c>null</c> mientras la cuenta
    /// está pendiente de activación (por ejemplo, un coordinador invitado por
    /// el administrador de plataforma que todavía no estableció su
    /// contraseña). La contraseña nunca se almacena en texto plano.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Indica si la cuenta está activa. Permite bloquear o desactivar el
    /// acceso sin eliminar el registro ni la información histórica asociada.
    /// </summary>
    public bool Activo { get; set; }

    /// <summary>Roles asignados a este usuario.</summary>
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();

    /// <summary>Perfil de empleado de este usuario, si lo tiene.</summary>
    public Empleado? Empleado { get; set; }

    /// <summary>Perfil de conductor de este usuario, si lo tiene.</summary>
    public Conductor? Conductor { get; set; }

    /// <summary>Mensajes enviados por este usuario.</summary>
    public ICollection<Mensaje> Mensajes { get; set; } = new List<Mensaje>();

    /// <summary>Notificaciones dirigidas a este usuario.</summary>
    public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();

    /// <summary>
    /// Tokens de verificación (confirmación de correo o recuperación de
    /// contraseña) generados para este usuario.
    /// </summary>
    public ICollection<TokenVerificacion> TokensVerificacion { get; set; } = new List<TokenVerificacion>();
}
