using LFMova.Application.DTOs.Empleados;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa <see cref="IEliminacionPersonaServicio"/>: decide si la persona
/// puede eliminarse y con qué alcance (cuenta completa o solo esta empresa) y
/// delega el borrado en <see cref="IEliminacionPersonaRepositorio"/>. No
/// accede directamente a Entity Framework Core.
/// </summary>
public class EliminacionPersonaServicio : IEliminacionPersonaServicio
{
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IEliminacionPersonaRepositorio _eliminacionRepositorio;
    private readonly IAlmacenamientoArchivos _almacenamiento;
    private readonly IHasheadorContrasenas _hasheadorContrasenas;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public EliminacionPersonaServicio(
        IEmpleadoRepositorio empleadoRepositorio,
        IUsuarioRepositorio usuarioRepositorio,
        IEliminacionPersonaRepositorio eliminacionRepositorio,
        IAlmacenamientoArchivos almacenamiento,
        IHasheadorContrasenas hasheadorContrasenas)
    {
        _empleadoRepositorio = empleadoRepositorio;
        _usuarioRepositorio = usuarioRepositorio;
        _eliminacionRepositorio = eliminacionRepositorio;
        _almacenamiento = almacenamiento;
        _hasheadorContrasenas = hasheadorContrasenas;
    }

    /// <inheritdoc />
    public async Task<ResultadoEliminacionPersonaDto> EliminarAsync(int empresaId, int empleadoId, int usuarioSolicitanteId)
    {
        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(empleadoId);
        if (empleado is null || !ReglasMultiempresa.EmpleadoPerteneceAEmpresa(empleado, empresaId))
        {
            throw new InvalidOperationException("El empleado indicado no existe en esta empresa.");
        }

        if (empleado.UsuarioId == usuarioSolicitanteId)
        {
            throw new InvalidOperationException("No puedes eliminar tu propia cuenta.");
        }

        var usuario = empleado.Usuario is null ? null : await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(empleado.Usuario.Cedula);
        if (usuario is not null && usuario.UsuarioRoles.Any(r => r.Activo && r.Rol is Rol.COORDINADOR or Rol.ADMINISTRADOR_PLATAFORMA))
        {
            throw new InvalidOperationException("Esta persona es coordinadora o administradora: quítale primero ese rol para poder eliminarla.");
        }

        if (await _eliminacionRepositorio.TieneRutaEnCursoAsync(empleado.UsuarioId))
        {
            throw new InvalidOperationException("Esta persona tiene una ruta en curso: espera a que finalice para eliminarla.");
        }

        var empresas = await _eliminacionRepositorio.ObtenerEmpresasRelacionadasAsync(empleado.UsuarioId);
        var soloDeEstaEmpresa = empresas.All(e => e == empresaId);

        var archivos = soloDeEstaEmpresa
            ? await _eliminacionRepositorio.EliminarCuentaAsync(empleado.UsuarioId)
            : await _eliminacionRepositorio.QuitarDeEmpresaAsync(empleado.UsuarioId, empresaId);
        await BorrarArchivosAsync(archivos);

        return new ResultadoEliminacionPersonaDto { CuentaEliminada = soloDeEstaEmpresa };
    }

    /// <inheritdoc />
    public async Task EliminarPropiaCuentaAsync(int usuarioId, string contrasena)
    {
        var usuario = await _usuarioRepositorio.ObtenerPorIdAsync(usuarioId);
        if (usuario is null)
        {
            throw new InvalidOperationException("La cuenta no existe.");
        }

        if (usuario.PasswordHash is null || !_hasheadorContrasenas.Verificar(contrasena, usuario.PasswordHash))
        {
            throw new InvalidOperationException("La contraseña es incorrecta.");
        }

        if (await _eliminacionRepositorio.TieneRutaEnCursoAsync(usuarioId))
        {
            throw new InvalidOperationException("Tienes una ruta en curso: espera a que finalice para eliminar tu cuenta.");
        }

        // Sin ningún administrador nadie podría volver a crear empresas ni asignar coordinadores.
        var esAdministrador = usuario.UsuarioRoles.Any(r => r.Activo && r.Rol == Rol.ADMINISTRADOR_PLATAFORMA);
        if (esAdministrador && await _eliminacionRepositorio.ContarOtrosAdministradoresAsync(usuarioId) == 0)
        {
            throw new InvalidOperationException("Eres la única persona administradora de la plataforma: no puedes eliminar tu cuenta.");
        }

        await BorrarArchivosAsync(await _eliminacionRepositorio.EliminarCuentaAsync(usuarioId));
    }

    private async Task BorrarArchivosAsync(List<string> referencias)
    {
        foreach (var referencia in referencias)
        {
            await _almacenamiento.EliminarAsync(referencia);
        }
    }
}
