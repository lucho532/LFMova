using TransportApp.Application.DTOs.Cuenta;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Validators;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa los casos de uso sobre la propia cuenta del usuario: datos de
/// contacto y cambio de contraseña. No modifica la cédula ni el correo (este
/// último exigiría una nueva confirmación).
/// </summary>
public class CuentaServicio : ICuentaServicio
{
    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IHasheadorContrasenas _hasheadorContrasenas;
    private readonly IPersonaServicio? _personaServicio;

    /// <summary>Crea el servicio con sus dependencias. <paramref name="personaServicio"/> permite mostrar las empresas de la persona.</summary>
    public CuentaServicio(IUsuarioRepositorio usuarioRepositorio, IHasheadorContrasenas hasheadorContrasenas, IPersonaServicio? personaServicio = null)
    {
        _usuarioRepositorio = usuarioRepositorio;
        _hasheadorContrasenas = hasheadorContrasenas;
        _personaServicio = personaServicio;
    }

    /// <inheritdoc />
    public async Task<CuentaDto> ObtenerAsync(int usuarioId)
    {
        var usuario = await ObtenerUsuarioOFallarAsync(usuarioId);
        var cuenta = ACuentaDto(usuario);

        if (_personaServicio is not null)
        {
            var persona = await _personaServicio.BuscarPorCedulaAsync(usuario.Cedula);
            cuenta.Empresas = new[] { persona.EmpresaCoordinada?.Nombre, persona.EmpresaEmpleado?.Nombre }
                .Concat(persona.EmpresasConductor.Select(e => e.Nombre))
                .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
                .Select(nombre => nombre!)
                .Distinct()
                .ToList();
        }

        return cuenta;
    }

    /// <inheritdoc />
    public async Task<CuentaDto> ActualizarAsync(int usuarioId, ActualizarCuentaDto datos)
    {
        if (string.IsNullOrWhiteSpace(datos.NombreCompleto))
        {
            throw new InvalidOperationException("El nombre completo es obligatorio.");
        }

        if (!TelefonoValidador.EsValido(datos.Telefono))
        {
            throw new InvalidOperationException("El teléfono es obligatorio.");
        }

        var usuario = await ObtenerUsuarioOFallarAsync(usuarioId);
        usuario.NombreCompleto = datos.NombreCompleto.Trim();
        usuario.Telefono = datos.Telefono.Trim();
        await _usuarioRepositorio.GuardarCambiosAsync();

        return ACuentaDto(usuario);
    }

    /// <inheritdoc />
    public async Task CambiarContrasenaAsync(int usuarioId, CambiarContrasenaDto datos)
    {
        if (datos.NuevaContrasena != datos.ConfirmacionContrasena)
        {
            throw new InvalidOperationException("Las contraseñas no coinciden.");
        }

        if (!PasswordValidador.EsValida(datos.NuevaContrasena))
        {
            throw new InvalidOperationException("La nueva contraseña es obligatoria.");
        }

        var usuario = await ObtenerUsuarioOFallarAsync(usuarioId);

        if (usuario.PasswordHash is null || !_hasheadorContrasenas.Verificar(datos.ContrasenaActual, usuario.PasswordHash))
        {
            throw new InvalidOperationException("La contraseña actual es incorrecta.");
        }

        if (datos.NuevaContrasena == datos.ContrasenaActual)
        {
            throw new InvalidOperationException("La nueva contraseña debe ser distinta de la actual.");
        }

        usuario.PasswordHash = _hasheadorContrasenas.Hashear(datos.NuevaContrasena);
        await _usuarioRepositorio.GuardarCambiosAsync();
    }

    private async Task<Usuario> ObtenerUsuarioOFallarAsync(int usuarioId)
    {
        var usuario = await _usuarioRepositorio.ObtenerPorIdAsync(usuarioId);
        return usuario ?? throw new InvalidOperationException("El usuario indicado no existe.");
    }

    private static CuentaDto ACuentaDto(Usuario usuario) => new()
    {
        Cedula = usuario.Cedula,
        NombreCompleto = usuario.NombreCompleto,
        Email = usuario.Email,
        Telefono = usuario.Telefono
    };
}
