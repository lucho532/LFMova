using TransportApp.Application.DTOs.Autenticacion;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define el caso de uso de recuperación de contraseña mediante un enlace
/// enviado por correo. El mismo mecanismo de token se reutiliza para que una
/// persona invitada (por ejemplo, el primer coordinador de una empresa)
/// establezca su contraseña inicial (ver <see cref="IEmpresaServicio.CrearAsync"/>).
/// No decide autorización: eso se verifica en la capa de Api.
/// </summary>
public interface IRecuperacionContrasenaServicio
{
    /// <summary>
    /// Solicita el envío de un enlace de recuperación de contraseña. No
    /// revela si la cédula/correo existe o no en la plataforma: si no existe
    /// ninguna cuenta, simplemente no se envía ningún correo.
    /// </summary>
    Task SolicitarAsync(SolicitarRecuperacionDto datos);

    /// <summary>
    /// Establece la nueva contraseña de la cuenta a partir del token
    /// recibido por correo, y marca el correo de la cuenta como confirmado
    /// (llegar hasta aquí implica haber recibido y abierto ese correo). Falla
    /// si el token no es válido, expiró o ya fue utilizado, o si las
    /// contraseñas no coinciden.
    /// </summary>
    Task RestablecerAsync(RestablecerContrasenaDto datos);

    /// <summary>
    /// Genera un enlace para establecer la contraseña inicial de una cuenta
    /// recién creada por invitación (por ejemplo, el primer coordinador de
    /// una empresa) y se lo envía por correo. A diferencia de
    /// <see cref="SolicitarAsync"/>, aquí ya se sabe con certeza que la
    /// cuenta existe y tiene correo (se acaba de crear en la misma
    /// operación), así que no aplica la misma cautela de "no revelar si
    /// existe".
    /// </summary>
    Task EnviarInvitacionAsync(Usuario usuarioInvitado, string contextoInvitacion);
}
