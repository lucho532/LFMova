using Microsoft.AspNetCore.Authorization;
using TransportApp.Domain.Enums;
using TransportApp.Infrastructure.Autenticacion;

namespace TransportApp.Api.Configuration;

/// <summary>
/// Representa el requisito de autorización de que el usuario autenticado
/// tenga un rol determinado activo (según los claims incluidos en su JWT).
/// No verifica el contexto de empresa; para eso ver
/// <see cref="ClaimsPrincipalExtensions.TieneRolEnEmpresa"/>.
/// </summary>
public class RequisitoRol : IAuthorizationRequirement
{
    /// <summary>Rol requerido para satisfacer este requisito.</summary>
    public Rol Rol { get; }

    /// <summary>Crea el requisito para el rol indicado.</summary>
    public RequisitoRol(Rol rol)
    {
        Rol = rol;
    }
}

/// <summary>
/// Evalúa <see cref="RequisitoRol"/> comprobando los claims de rol emitidos
/// por <see cref="GeneradorTokenJwt"/>. No consulta persistencia: confía
/// únicamente en el contenido ya validado del JWT.
/// </summary>
public class ManejadorRequisitoRol : AuthorizationHandler<RequisitoRol>
{
    /// <summary>Determina si el usuario autenticado cumple el requisito de rol.</summary>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequisitoRol requirement)
    {
        var tieneElRol = context.User.Claims.Any(c =>
            c.Type == GeneradorTokenJwt.ClaimRol &&
            (c.Value == requirement.Rol.ToString() || c.Value.StartsWith(requirement.Rol + ":", StringComparison.Ordinal)));

        if (tieneElRol)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
