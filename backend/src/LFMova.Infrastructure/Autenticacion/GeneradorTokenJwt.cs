using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;

namespace LFMova.Infrastructure.Autenticacion;

/// <summary>
/// Genera tokens JWT firmados que representan la identidad del usuario y sus
/// roles activos. Cada rol activo se incluye como un claim propio; cuando el
/// rol tiene una empresa asociada, esa empresa se codifica junto al rol
/// (formato <c>"ROL:EMPRESAID"</c> o <c>"ROL"</c> cuando el rol es global).
/// No decide la estrategia de selección de "rol activo" en el cliente: eso
/// permanece como decisión pendiente (ver <c>spec.md</c> §47).
/// </summary>
public class GeneradorTokenJwt : IGeneradorTokenJwt
{
    /// <summary>Nombre del claim que representa cada rol (con su empresa, cuando aplica).</summary>
    public const string ClaimRol = "rol";

    /// <summary>
    /// Nombre del claim propio que identifica al usuario. Se usa en lugar del
    /// claim estándar "sub" para evitar el remapeo automático de claims que
    /// aplica el manejador de JWT de ASP.NET Core sobre los nombres de claim
    /// registrados.
    /// </summary>
    public const string ClaimUsuarioId = "usuarioId";

    private readonly OpcionesJwt _opciones;

    /// <summary>Crea el generador utilizando las opciones de configuración de JWT.</summary>
    public GeneradorTokenJwt(IOptions<OpcionesJwt> opciones)
    {
        _opciones = opciones.Value;
    }

    /// <inheritdoc />
    public (string Token, DateTime ExpiraEnUtc) GenerarToken(Usuario usuario, IEnumerable<UsuarioRol> rolesActivos)
    {
        var expiraEnUtc = DateTime.UtcNow.AddMinutes(_opciones.ExpiracionMinutos);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.UsuarioId.ToString()),
            new(ClaimUsuarioId, usuario.UsuarioId.ToString()),
            new("cedula", usuario.Cedula),
            new("nombre", usuario.NombreCompleto),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(rolesActivos.Select(rol => new Claim(
            ClaimRol,
            rol.EmpresaId is null ? rol.Rol.ToString() : $"{rol.Rol}:{rol.EmpresaId}")));

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.ClaveSecreta));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opciones.Emisor,
            audience: _opciones.Audiencia,
            claims: claims,
            expires: expiraEnUtc,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEnUtc);
    }
}
