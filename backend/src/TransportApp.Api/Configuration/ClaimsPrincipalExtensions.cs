using System.Security.Claims;
using TransportApp.Domain.Enums;
using TransportApp.Infrastructure.Autenticacion;

namespace TransportApp.Api.Configuration;

/// <summary>
/// Métodos de extensión para verificar, a partir de los claims del JWT, el
/// contexto de empresa autorizado del usuario autenticado. Los controladores
/// deben usarlos para comprobar que un usuario tiene el rol indicado
/// específicamente para la empresa del recurso solicitado; nunca deben
/// confiar únicamente en un <c>EmpresaId</c> recibido en la petición.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Indica si el usuario autenticado tiene el rol indicado activo para la
    /// empresa indicada.
    /// </summary>
    public static bool TieneRolEnEmpresa(this ClaimsPrincipal usuario, Rol rol, int empresaId)
        => usuario.Claims.Any(c =>
            c.Type == GeneradorTokenJwt.ClaimRol &&
            c.Value == $"{rol}:{empresaId}");

    /// <summary>
    /// Indica si el usuario autenticado tiene activo el rol global indicado
    /// (sin empresa asociada, p. ej. <c>CONDUCTOR</c> o
    /// <c>ADMINISTRADOR_PLATAFORMA</c>).
    /// </summary>
    public static bool TieneRolGlobal(this ClaimsPrincipal usuario, Rol rol)
        => usuario.Claims.Any(c =>
            c.Type == GeneradorTokenJwt.ClaimRol &&
            c.Value == rol.ToString());

    /// <summary>
    /// Indica si el usuario autenticado es, mediante el claim de identidad
    /// del token, el usuario indicado.
    /// </summary>
    public static bool EsUsuario(this ClaimsPrincipal usuario, int usuarioId)
        => usuario.Claims.Any(c =>
            c.Type == GeneradorTokenJwt.ClaimUsuarioId &&
            c.Value == usuarioId.ToString());

    /// <summary>
    /// Obtiene el <c>UsuarioId</c> del usuario autenticado a partir del claim
    /// de identidad del token.
    /// </summary>
    public static int ObtenerUsuarioId(this ClaimsPrincipal usuario)
        => int.Parse(usuario.Claims.First(c => c.Type == GeneradorTokenJwt.ClaimUsuarioId).Value);
}
