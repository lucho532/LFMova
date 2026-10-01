using TransportApp.Domain.Entities;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define el contrato para generar el token JWT que representa la identidad
/// y los roles autorizados de un usuario autenticado. No decide si el
/// usuario puede autenticarse; solo emite el token una vez que la capa de
/// aplicación ya validó las credenciales.
/// </summary>
public interface IGeneradorTokenJwt
{
    /// <summary>
    /// Genera un token JWT para el usuario indicado, representando sus
    /// roles activos (y la empresa asociada a cada uno, cuando aplique).
    /// Devuelve el token y su momento de expiración en UTC.
    /// </summary>
    (string Token, DateTime ExpiraEnUtc) GenerarToken(Usuario usuario, IEnumerable<UsuarioRol> rolesActivos);
}
