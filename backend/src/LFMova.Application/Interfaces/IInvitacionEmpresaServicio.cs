using LFMova.Application.DTOs.Invitaciones;
using LFMova.Domain.Entities;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define los casos de uso de invitación por correo a una empresa: el
/// coordinador invita por cédula, la persona recibe un enlace en su propio
/// correo y, al aceptarlo (o al registrarse desde él), queda como parte de la
/// empresa para que el coordinador pueda asignarle un rol. No decide
/// autorización por rol: eso se verifica en la capa de Api.
/// </summary>
public interface IInvitacionEmpresaServicio
{
    /// <summary>
    /// Envía una invitación a la cédula indicada. Nunca revela al coordinador
    /// si la cédula ya tenía cuenta ni a qué correo se envió.
    /// </summary>
    Task InvitarAsync(int empresaId, CrearInvitacionEmpresaDto datos, int usuarioInvitadorId);

    /// <summary>Invitaciones enviadas por la empresa, de la más reciente a la más antigua.</summary>
    Task<List<InvitacionEmpresaDto>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Detalle de la invitación para quien abre el enlace del correo. Falla si el token no es válido.</summary>
    Task<DetalleInvitacionDto> ObtenerDetalleAsync(string token);

    /// <summary>La persona autenticada (dueña de la cédula invitada) acepta la invitación.</summary>
    Task AceptarAsync(string token, int usuarioId);

    /// <summary>
    /// Obtiene la invitación del token si todavía puede aceptarse y es para
    /// la cédula indicada; si no, falla. Lo usa el registro desde el enlace.
    /// </summary>
    Task<InvitacionEmpresa> ObtenerParaRegistroAsync(string token, string cedula);

    /// <summary>Marca la invitación como aceptada por el usuario y lo une a la empresa como empleado.</summary>
    Task AceptarParaUsuarioAsync(InvitacionEmpresa invitacion, Usuario usuario);
}
