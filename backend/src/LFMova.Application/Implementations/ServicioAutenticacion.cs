using LFMova.Application.DTOs.Autenticacion;
using LFMova.Application.Interfaces;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa el caso de uso de autenticación mediante cédula y contraseña.
/// Coordina el repositorio de usuarios, el verificador de contraseñas y el
/// generador de tokens JWT. No accede directamente a Entity Framework Core
/// ni a ningún proveedor de persistencia concreto.
/// </summary>
public class ServicioAutenticacion : IServicioAutenticacion
{
    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IHasheadorContrasenas _hasheadorContrasenas;
    private readonly IGeneradorTokenJwt _generadorTokenJwt;

    /// <summary>Crea el servicio de autenticación con sus dependencias.</summary>
    public ServicioAutenticacion(
        IUsuarioRepositorio usuarioRepositorio,
        IHasheadorContrasenas hasheadorContrasenas,
        IGeneradorTokenJwt generadorTokenJwt)
    {
        _usuarioRepositorio = usuarioRepositorio;
        _hasheadorContrasenas = hasheadorContrasenas;
        _generadorTokenJwt = generadorTokenJwt;
    }

    /// <inheritdoc />
    public async Task<RespuestaAutenticacionDto> IniciarSesionAsync(IniciarSesionDto datos)
    {
        var usuario = await _usuarioRepositorio.ObtenerPorIdentificadorConRolesAsync(datos.Identificador);

        if (usuario is null)
        {
            throw new InvalidOperationException("Cédula/correo o contraseña incorrecta.");
        }

        if (!usuario.Activo)
        {
            throw new InvalidOperationException("La cuenta está inactiva.");
        }

        if (usuario.PasswordHash is null)
        {
            throw new InvalidOperationException("La cuenta está pendiente de activación. Revisa tu correo para establecer tu contraseña.");
        }

        if (!_hasheadorContrasenas.Verificar(datos.Password, usuario.PasswordHash))
        {
            throw new InvalidOperationException("Cédula/correo o contraseña incorrecta.");
        }

        if (!usuario.CorreoConfirmado)
        {
            throw new InvalidOperationException("Debes confirmar tu correo electrónico antes de iniciar sesión.");
        }

        var rolesActivos = usuario.UsuarioRoles.Where(r => r.Activo).ToList();

        if (rolesActivos.Count == 0)
        {
            throw new InvalidOperationException("El usuario no tiene roles activos.");
        }

        var (token, expiraEnUtc) = _generadorTokenJwt.GenerarToken(usuario, rolesActivos);

        return new RespuestaAutenticacionDto
        {
            Token = token,
            ExpiraEnUtc = expiraEnUtc
        };
    }
}
