using TransportApp.Application.DTOs.Autenticacion;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define el caso de uso de autenticación de un usuario mediante cédula y
/// contraseña.
/// </summary>
public interface IServicioAutenticacion
{
    /// <summary>
    /// Valida las credenciales del usuario (existencia, cuenta activa,
    /// contraseña correcta y al menos un rol activo) y, si son válidas,
    /// devuelve un token JWT.
    /// </summary>
    Task<RespuestaAutenticacionDto> IniciarSesionAsync(IniciarSesionDto datos);
}
