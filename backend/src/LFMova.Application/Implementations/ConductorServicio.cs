using LFMova.Application.DTOs.Conductores;
using LFMova.Application.DTOs.Servicios;
using LFMova.Application.Implementations.Conductores;
using LFMova.Application.Interfaces;
using LFMova.Application.Mappers;
using LFMova.Application.Validators;
using LFMova.Domain.Entities;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de registro de conductores y su vinculación
/// con empresas. Un conductor se identifica de forma única por la cédula de
/// su <c>Usuario</c>; nunca se crea un segundo <c>Conductor</c> para la misma
/// persona (ver <see cref="ReglasVinculacionConductorEmpresa"/>). No decide
/// autorización: eso se verifica en la capa de Api.
/// </summary>
public class ConductorServicio : IConductorServicio
{
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IEmpresaRepositorio _empresaRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IVehiculoRepositorio _vehiculoRepositorio;
    private readonly RegistroConductor _registro;

    /// <summary>Crea el servicio con sus repositorios y colaboradores.</summary>
    public ConductorServicio(
        IConductorRepositorio conductorRepositorio,
        IUsuarioRepositorio usuarioRepositorio,
        IEmpresaRepositorio empresaRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IServicioRepositorio servicioRepositorio,
        IVehiculoRepositorio vehiculoRepositorio,
        RegistroConductor registro)
    {
        _conductorRepositorio = conductorRepositorio;
        _usuarioRepositorio = usuarioRepositorio;
        _empresaRepositorio = empresaRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _servicioRepositorio = servicioRepositorio;
        _vehiculoRepositorio = vehiculoRepositorio;
        _registro = registro;
    }

    /// <inheritdoc />
    public Task<ConductorDto> CrearAsync(int empresaId, CrearConductorDto datos)
        => _registro.CrearAsync(empresaId, datos);

    /// <inheritdoc />
    public async Task<VinculacionConductorEmpresaDto> VincularAsync(int empresaId, VincularConductorDto datos)
    {
        if (!CedulaValidador.EsValida(datos.Cedula))
        {
            throw new InvalidOperationException("La cédula del conductor es obligatoria.");
        }

        var empresa = await _empresaRepositorio.ObtenerPorIdAsync(empresaId);
        if (empresa is null)
        {
            throw new InvalidOperationException("La empresa indicada no existe.");
        }

        var usuario = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(datos.Cedula);
        var conductor = usuario is null ? null : await _conductorRepositorio.ObtenerPorUsuarioIdAsync(usuario.UsuarioId);

        if (conductor is null)
        {
            throw new InvalidOperationException("No existe un conductor registrado con esta cédula.");
        }

        if (ReglasVinculacionConductorEmpresa.YaExisteVinculacion(conductor.VinculacionesConductorEmpresa, empresaId))
        {
            throw new InvalidOperationException("El conductor ya tiene una vinculación con esta empresa.");
        }

        var vinculacion = new VinculacionConductorEmpresa
        {
            ConductorId = conductor.ConductorId,
            EmpresaId = empresaId,
            Activa = true
        };

        await _conductorRepositorio.AgregarVinculacionAsync(vinculacion);
        await _conductorRepositorio.GuardarCambiosAsync();

        return ConductorMapper.AVinculacionDto(vinculacion);
    }

    /// <inheritdoc />
    public async Task<ConductorDto?> ObtenerPorIdAsync(int conductorId)
    {
        var conductor = await _conductorRepositorio.ObtenerPorIdAsync(conductorId);
        return conductor is null ? null : ConductorMapper.AConductorDto(conductor);
    }

    /// <inheritdoc />
    public async Task<List<ConductorDto>> ObtenerPorEmpresaAsync(int empresaId)
    {
        var conductores = await _conductorRepositorio.ObtenerPorEmpresaAsync(empresaId);
        return conductores.Select(ConductorMapper.AConductorDto).ToList();
    }

    /// <inheritdoc />
    public async Task ActivarVinculacionAsync(int empresaId, int conductorId)
    {
        var vinculacion = await ObtenerVinculacionOFallarAsync(empresaId, conductorId);
        vinculacion.Activa = true;
        await _conductorRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task DesactivarVinculacionAsync(int empresaId, int conductorId)
    {
        var vinculacion = await ObtenerVinculacionOFallarAsync(empresaId, conductorId);
        vinculacion.Activa = false;
        await _conductorRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task<List<ServicioDto>> ObtenerServiciosPropiosAsync(int usuarioId)
    {
        var conductor = await _conductorRepositorio.ObtenerPorUsuarioIdAsync(usuarioId);
        if (conductor is null)
        {
            throw new InvalidOperationException("El usuario autenticado no tiene un perfil de conductor.");
        }

        var unidades = await _unidadOperativaRepositorio.ObtenerPorConductorAsync(conductor.ConductorId);

        var servicios = new List<Servicio>();
        foreach (var unidad in unidades)
        {
            servicios.AddRange(await _servicioRepositorio.ObtenerPorUnidadOperativaAsync(unidad.UnidadOperativaId));
        }

        return servicios
            .Where(s => ReglasEstadoServicio.EsVisibleParaConductorYEmpleado(s.Estado))
            .OrderByDescending(s => s.Fecha)
            .ThenByDescending(s => s.HoraProgramada)
            .Select(s => ServicioMapper.AServicioDto(s, s.Jornada!.EmpresaId))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<UnidadDeTrabajoDto>> ObtenerUnidadesPropiasAsync(int usuarioId)
    {
        var conductor = await _conductorRepositorio.ObtenerPorUsuarioIdAsync(usuarioId);
        if (conductor is null)
        {
            throw new InvalidOperationException("El usuario autenticado no tiene un perfil de conductor.");
        }

        var unidades = await _unidadOperativaRepositorio.ObtenerPorConductorAsync(conductor.ConductorId);
        var vehiculos = await _vehiculoRepositorio.ObtenerPorConductorAsync(conductor.ConductorId);

        return unidades
            .Where(u => vehiculos.Any(v => v.VehiculoId == u.VehiculoId))
            .Select(u => new UnidadDeTrabajoDto
            {
                UnidadOperativaId = u.UnidadOperativaId,
                Activa = u.Activa,
                Vehiculo = VehiculoMapper.AVehiculoDto(vehiculos.First(v => v.VehiculoId == u.VehiculoId))
            })
            .ToList();
    }

    private async Task<Conductor> ObtenerConductorOFallarAsync(int conductorId)
    {
        var conductor = await _conductorRepositorio.ObtenerPorIdAsync(conductorId);
        if (conductor is null)
        {
            throw new InvalidOperationException("El conductor indicado no existe.");
        }

        return conductor;
    }

    private async Task<VinculacionConductorEmpresa> ObtenerVinculacionOFallarAsync(int empresaId, int conductorId)
    {
        var conductor = await ObtenerConductorOFallarAsync(conductorId);
        var vinculacion = conductor.VinculacionesConductorEmpresa.FirstOrDefault(v => v.EmpresaId == empresaId);

        if (vinculacion is null)
        {
            throw new InvalidOperationException("El conductor no tiene una vinculación con esta empresa.");
        }

        return vinculacion;
    }
}
