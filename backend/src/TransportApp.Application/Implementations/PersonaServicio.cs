using TransportApp.Application.DTOs.Personas;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Validators;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa la búsqueda de personas por cédula y la regla de qué personas
/// puede gestionar un coordinador (las que ya forman parte de su empresa).
/// Solo lee: nunca crea ni modifica cuentas ni roles.
/// </summary>
public class PersonaServicio : IPersonaServicio
{
    private const string MensajeNoEncontrada = "No existe ninguna persona registrada con esa cédula.";

    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IEmpresaRepositorio _empresaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IVehiculoRepositorio _vehiculoRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly IInvitacionEmpresaRepositorio _invitacionRepositorio;

    /// <summary>Crea el servicio con sus repositorios.</summary>
    public PersonaServicio(
        IUsuarioRepositorio usuarioRepositorio,
        IEmpresaRepositorio empresaRepositorio,
        IConductorRepositorio conductorRepositorio,
        IVehiculoRepositorio vehiculoRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IInvitacionEmpresaRepositorio invitacionRepositorio)
    {
        _usuarioRepositorio = usuarioRepositorio;
        _empresaRepositorio = empresaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _vehiculoRepositorio = vehiculoRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _invitacionRepositorio = invitacionRepositorio;
    }

    /// <inheritdoc />
    public async Task<bool> EstaRelacionadaConEmpresaAsync(string cedula, int empresaId)
    {
        var usuario = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(cedula.Trim());
        return usuario is not null && await EstaRelacionadaAsync(usuario, empresaId);
    }

    /// <inheritdoc />
    public async Task<bool> PuedeGestionarseDesdeEmpresaAsync(string cedula, int empresaId)
    {
        var usuario = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(cedula.Trim());
        return usuario is null || await EstaRelacionadaAsync(usuario, empresaId);
    }

    /// <inheritdoc />
    public async Task<PersonaDto> BuscarPorCedulaAsync(string cedula, int? empresaIdRestringida = null)
    {
        if (!CedulaValidador.EsValida(cedula))
        {
            throw new InvalidOperationException("La cédula es obligatoria.");
        }

        var usuario = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(cedula.Trim());
        if (usuario is null)
        {
            throw new InvalidOperationException(MensajeNoEncontrada);
        }

        if (empresaIdRestringida is not null && !await EstaRelacionadaAsync(usuario, empresaIdRestringida.Value))
        {
            throw new InvalidOperationException(MensajeNoEncontrada);
        }

        var rolesActivos = usuario.UsuarioRoles.Where(r => r.Activo).ToList();

        var persona = new PersonaDto
        {
            Cedula = usuario.Cedula,
            NombreCompleto = usuario.NombreCompleto,
            Email = usuario.Email,
            Telefono = usuario.Telefono,
            EsConductor = rolesActivos.Any(r => r.Rol == Rol.CONDUCTOR),
            EsCoordinador = rolesActivos.Any(r => r.Rol == Rol.COORDINADOR)
        };

        var rolCoordinador = rolesActivos.FirstOrDefault(r => r.Rol == Rol.COORDINADOR && r.EmpresaId is not null);
        if (rolCoordinador is not null)
        {
            persona.EmpresaCoordinada = await ObtenerResumenAsync(rolCoordinador.EmpresaId!.Value);
        }

        var conductor = await _conductorRepositorio.ObtenerPorUsuarioIdAsync(usuario.UsuarioId);
        if (conductor is not null)
        {
            foreach (var vinculacion in conductor.VinculacionesConductorEmpresa.Where(v => v.Activa))
            {
                var resumen = await ObtenerResumenAsync(vinculacion.EmpresaId);
                if (resumen is not null)
                {
                    persona.EmpresasConductor.Add(resumen);
                }
            }

            persona.Placas = (await _vehiculoRepositorio.ObtenerPorConductorAsync(conductor.ConductorId))
                .Where(v => v.Activo).Select(v => v.Placa).ToList();
        }

        var empleado = await _empleadoRepositorio.ObtenerPorUsuarioIdAsync(usuario.UsuarioId);
        if (empleado is not null)
        {
            persona.EmpresaEmpleado = await ObtenerResumenAsync(empleado.EmpresaId);
        }

        return persona;
    }

    /// <summary>
    /// Una persona forma parte de la empresa si la coordina, es su empleado,
    /// está vinculada a ella como conductor o aceptó una invitación suya.
    /// </summary>
    private async Task<bool> EstaRelacionadaAsync(Usuario usuario, int empresaId)
    {
        if (usuario.UsuarioRoles.Any(r => r.Activo && r.Rol == Rol.COORDINADOR && r.EmpresaId == empresaId))
        {
            return true;
        }

        var empleado = await _empleadoRepositorio.ObtenerPorUsuarioIdAsync(usuario.UsuarioId);
        if (empleado?.EmpresaId == empresaId)
        {
            return true;
        }

        var conductor = await _conductorRepositorio.ObtenerPorUsuarioIdAsync(usuario.UsuarioId);
        if (conductor is not null && conductor.VinculacionesConductorEmpresa.Any(v => v.EmpresaId == empresaId))
        {
            return true;
        }

        var invitaciones = await _invitacionRepositorio.ObtenerPorEmpresaYCedulaAsync(empresaId, usuario.Cedula);
        return invitaciones.Any(i => i.FechaAceptacion is not null && i.UsuarioAceptanteId == usuario.UsuarioId);
    }

    private async Task<EmpresaResumenDto?> ObtenerResumenAsync(int empresaId)
    {
        var empresa = await _empresaRepositorio.ObtenerPorIdAsync(empresaId);
        return empresa is null ? null : new EmpresaResumenDto { EmpresaId = empresa.EmpresaId, Nombre = empresa.Nombre };
    }
}
