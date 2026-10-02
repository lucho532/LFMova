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

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public EliminacionPersonaServicio(
        IEmpleadoRepositorio empleadoRepositorio,
        IUsuarioRepositorio usuarioRepositorio,
        IEliminacionPersonaRepositorio eliminacionRepositorio,
        IAlmacenamientoArchivos almacenamiento)
    {
        _empleadoRepositorio = empleadoRepositorio;
        _usuarioRepositorio = usuarioRepositorio;
        _eliminacionRepositorio = eliminacionRepositorio;
        _almacenamiento = almacenamiento;
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

        List<string> archivos;
        if (soloDeEstaEmpresa)
        {
            if (await _eliminacionRepositorio.TieneRegistrosDeCoordinacionAsync(empleado.UsuarioId))
            {
                throw new InvalidOperationException("Esta persona registró importaciones o invitaciones como coordinadora y su cuenta no puede eliminarse.");
            }

            archivos = await _eliminacionRepositorio.EliminarCuentaAsync(empleado.UsuarioId);
        }
        else
        {
            archivos = await _eliminacionRepositorio.QuitarDeEmpresaAsync(empleado.UsuarioId, empresaId);
        }

        foreach (var referencia in archivos)
        {
            await _almacenamiento.EliminarAsync(referencia);
        }

        return new ResultadoEliminacionPersonaDto { CuentaEliminada = soloDeEstaEmpresa };
    }
}
