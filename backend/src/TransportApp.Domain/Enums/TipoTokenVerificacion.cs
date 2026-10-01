namespace TransportApp.Domain.Enums;

/// <summary>
/// Representa el propósito de un <c>TokenVerificacion</c>: para qué acción
/// habilita el enlace enviado por correo.
/// </summary>
public enum TipoTokenVerificacion
{
    /// <summary>Confirma que el correo indicado en el registro pertenece a la persona.</summary>
    CONFIRMACION_CORREO,

    /// <summary>
    /// Permite establecer o restablecer la contraseña de la cuenta. Se usa
    /// tanto para la recuperación de contraseña de una cuenta activa como
    /// para que una persona invitada por un administrador o coordinador
    /// (por ejemplo, el primer coordinador de una empresa) establezca su
    /// contraseña inicial.
    /// </summary>
    ESTABLECER_CONTRASENA
}
