using LFMova.Application.DTOs.Autenticacion;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define el caso de uso de autorregistro de cualquier persona en la
/// plataforma y la confirmación de su correo electrónico. No decide
/// autorización: eso se verifica en la capa de Api.
/// </summary>
public interface IRegistroServicio
{
    /// <summary>
    /// Registra una nueva cuenta con el rol <c>EMPLEADO</c> sin empresa
    /// asignada, y envía un correo de confirmación. Falla si la cédula o el
    /// correo ya están registrados. Si trae un token de invitación válido
    /// para esa cédula, la cuenta queda unida a la empresa que invitó y, si
    /// el correo es el mismo al que llegó la invitación, ya confirmada (sin
    /// enviar el correo de confirmación).
    /// </summary>
    Task<ResultadoRegistroDto> RegistrarAsync(RegistrarUsuarioDto datos);

    /// <summary>
    /// Confirma el correo de una cuenta mediante el token recibido. Falla si
    /// el token no es válido, expiró o ya fue utilizado.
    /// </summary>
    Task ConfirmarCorreoAsync(ConfirmarCorreoDto datos);
}
