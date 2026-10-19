using LFMova.Application.DTOs.Empresas;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Application.Mappers;
using LFMova.Application.Validators;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa la asignación y revocación del rol <c>COORDINADOR</c> de una
/// empresa, aplicando las reglas de <see cref="ReglasUsuarioRol"/>. No
/// decide autorización de quién puede invocar estas operaciones: eso se
/// verifica en la capa de Api antes de llamar a este servicio.
/// </summary>
public class AsignacionCoordinadorServicio : IAsignacionCoordinadorServicio
{
    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IUsuarioRolRepositorio _usuarioRolRepositorio;
    private readonly IEmpresaRepositorio _empresaRepositorio;
    private readonly IRecuperacionContrasenaServicio _recuperacionContrasenaServicio;

    /// <summary>Crea el servicio con sus repositorios y el servicio de invitación por correo.</summary>
    public AsignacionCoordinadorServicio(
        IUsuarioRepositorio usuarioRepositorio,
        IUsuarioRolRepositorio usuarioRolRepositorio,
        IEmpresaRepositorio empresaRepositorio,
        IRecuperacionContrasenaServicio recuperacionContrasenaServicio)
    {
        _usuarioRepositorio = usuarioRepositorio;
        _usuarioRolRepositorio = usuarioRolRepositorio;
        _empresaRepositorio = empresaRepositorio;
        _recuperacionContrasenaServicio = recuperacionContrasenaServicio;
    }

    /// <inheritdoc />
    public async Task<List<CoordinadorDto>> ObtenerPorEmpresaAsync(int empresaId)
    {
        var roles = await _usuarioRolRepositorio.ObtenerPorEmpresaYRolAsync(empresaId, Rol.COORDINADOR);
        return roles.Select(CoordinadorMapper.ACoordinadorDto).ToList();
    }

    /// <inheritdoc />
    public async Task AsignarAsync(int empresaId, AsignarCoordinadorDto datos, int usuarioEjecutorId)
    {
        if (!CedulaValidador.EsValida(datos.Cedula))
        {
            throw new InvalidOperationException("La cédula del coordinador es obligatoria.");
        }

        var empresa = await _empresaRepositorio.ObtenerPorIdAsync(empresaId);
        if (empresa is null)
        {
            throw new InvalidOperationException("La empresa indicada no existe.");
        }

        var usuario = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(datos.Cedula);
        var usuarioEsNuevo = usuario is null;

        if (usuario is null)
        {
            if (string.IsNullOrWhiteSpace(datos.NombreCoordinador))
            {
                throw new InvalidOperationException("El nombre del coordinador es obligatorio para crear su cuenta.");
            }

            if (!TelefonoValidador.EsValido(datos.TelefonoCoordinador))
            {
                throw new InvalidOperationException("El teléfono del coordinador es obligatorio para crear su cuenta.");
            }

            if (!EmailValidador.EsValido(CorreoNormalizador.Normalizar(datos.CorreoCoordinador ?? string.Empty)))
            {
                throw new InvalidOperationException("El correo del coordinador es obligatorio para crear su cuenta.");
            }

            if (await _usuarioRepositorio.ObtenerPorEmailAsync(CorreoNormalizador.Normalizar(datos.CorreoCoordinador)) is not null)
            {
                throw new InvalidOperationException("Ya existe una cuenta registrada con este correo.");
            }

            usuario = new Usuario
            {
                Cedula = datos.Cedula,
                Email = CorreoNormalizador.Normalizar(datos.CorreoCoordinador),
                NombreCompleto = datos.NombreCoordinador,
                Telefono = datos.TelefonoCoordinador,
                PasswordHash = null,
                CorreoConfirmado = false,
                Activo = true
            };

            await _usuarioRepositorio.AgregarAsync(usuario);
            await _usuarioRepositorio.GuardarCambiosAsync();
        }

        if (ReglasUsuarioRol.EsAutoasignacion(usuarioEjecutorId, usuario.UsuarioId))
        {
            throw new InvalidOperationException("Un usuario no puede asignarse a sí mismo el rol de coordinador.");
        }

        var rolesActivos = usuario.UsuarioRoles.Where(r => r.Activo).ToList();

        if (ReglasUsuarioRol.YaEsCoordinadorDeEmpresa(rolesActivos, empresaId))
        {
            throw new InvalidOperationException("El usuario ya es coordinador de esta empresa.");
        }

        if (ReglasUsuarioRol.ViolaUnicidadDeCoordinador(rolesActivos, empresaId))
        {
            throw new InvalidOperationException("El usuario ya es coordinador activo de otra empresa.");
        }

        var nuevoRol = new UsuarioRol
        {
            UsuarioId = usuario.UsuarioId,
            Rol = Rol.COORDINADOR,
            EmpresaId = empresaId,
            Activo = true
        };

        await _usuarioRolRepositorio.AgregarAsync(nuevoRol);
        await _usuarioRolRepositorio.GuardarCambiosAsync();

        if (usuarioEsNuevo)
        {
            await _recuperacionContrasenaServicio.EnviarInvitacionAsync(usuario, $"como coordinador de {empresa.Nombre}");
        }
    }

    /// <inheritdoc />
    public async Task RevocarAsync(int empresaId, int usuarioRolId)
    {
        var rolARevocar = await _usuarioRolRepositorio.ObtenerPorIdAsync(usuarioRolId);

        if (rolARevocar is null || rolARevocar.Rol != Rol.COORDINADOR || rolARevocar.EmpresaId != empresaId)
        {
            throw new InvalidOperationException("El rol de coordinador indicado no existe para esta empresa.");
        }

        var rolesCoordinadorDeLaEmpresa = await _usuarioRolRepositorio.ObtenerPorEmpresaYRolAsync(empresaId, Rol.COORDINADOR);

        if (ReglasUsuarioRol.EsElUltimoCoordinadorActivo(rolesCoordinadorDeLaEmpresa, usuarioRolId))
        {
            throw new InvalidOperationException("No se puede revocar al último coordinador activo de la empresa.");
        }

        rolARevocar.Activo = false;
        await _usuarioRolRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task ReactivarAsync(int empresaId, int usuarioRolId)
    {
        var rolAReactivar = await _usuarioRolRepositorio.ObtenerPorIdAsync(usuarioRolId);

        if (rolAReactivar is null || rolAReactivar.Rol != Rol.COORDINADOR || rolAReactivar.EmpresaId != empresaId)
        {
            throw new InvalidOperationException("El rol de coordinador indicado no existe para esta empresa.");
        }

        if (rolAReactivar.Activo)
        {
            return;
        }

        var usuario = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(rolAReactivar.Usuario?.Cedula ?? string.Empty);
        var rolesActivosDelUsuario = usuario?.UsuarioRoles.Where(r => r.Activo) ?? Enumerable.Empty<UsuarioRol>();

        if (ReglasUsuarioRol.ViolaUnicidadDeCoordinador(rolesActivosDelUsuario, empresaId))
        {
            throw new InvalidOperationException("El usuario ya es coordinador activo de otra empresa.");
        }

        rolAReactivar.Activo = true;
        await _usuarioRolRepositorio.GuardarCambiosAsync();
    }
}
