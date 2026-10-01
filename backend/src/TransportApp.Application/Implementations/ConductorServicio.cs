using TransportApp.Application.DTOs.Conductores;
using TransportApp.Application.DTOs.Servicios;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Mappers;
using TransportApp.Application.Validators;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Domain.Rules;

namespace TransportApp.Application.Implementations;

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
    private readonly IUsuarioRolRepositorio _usuarioRolRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IVehiculoRepositorio _vehiculoRepositorio;

    /// <summary>Crea el servicio con sus repositorios.</summary>
    public ConductorServicio(
        IConductorRepositorio conductorRepositorio,
        IUsuarioRepositorio usuarioRepositorio,
        IEmpresaRepositorio empresaRepositorio,
        IUsuarioRolRepositorio usuarioRolRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IServicioRepositorio servicioRepositorio,
        IVehiculoRepositorio vehiculoRepositorio)
    {
        _conductorRepositorio = conductorRepositorio;
        _usuarioRepositorio = usuarioRepositorio;
        _empresaRepositorio = empresaRepositorio;
        _usuarioRolRepositorio = usuarioRolRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _servicioRepositorio = servicioRepositorio;
        _vehiculoRepositorio = vehiculoRepositorio;
    }

    /// <inheritdoc />
    public async Task<ConductorDto> CrearAsync(int empresaId, CrearConductorDto datos)
    {
        if (!CedulaValidador.EsValida(datos.Cedula))
        {
            throw new InvalidOperationException("La cédula del conductor es obligatoria.");
        }

        var errorDeVehiculo = VehiculoValidador.ObtenerError(datos.Vehiculo);
        if (errorDeVehiculo is not null)
        {
            throw new InvalidOperationException(errorDeVehiculo);
        }

        var empresa = await _empresaRepositorio.ObtenerPorIdAsync(empresaId);
        if (empresa is null)
        {
            throw new InvalidOperationException("La empresa indicada no existe.");
        }

        var usuario = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(datos.Cedula);
        if (usuario is null)
        {
            throw new InvalidOperationException(
                "No existe ninguna cuenta con esta cédula. La persona debe registrarse primero en la plataforma.");
        }

        if (await _vehiculoRepositorio.ObtenerPorPlacaAsync(datos.Vehiculo.Placa.Trim()) is not null)
        {
            throw new InvalidOperationException("Ya existe un vehículo registrado con esta placa.");
        }

        var conductorExistente = await _conductorRepositorio.ObtenerPorUsuarioIdAsync(usuario.UsuarioId);
        if (conductorExistente is not null)
        {
            throw new InvalidOperationException(
                "Ya existe un conductor registrado con esta cédula. Utilice la vinculación con la empresa.");
        }

        var conductor = new Conductor
        {
            UsuarioId = usuario.UsuarioId,
            NombreCompleto = usuario.NombreCompleto,
            Telefono = usuario.Telefono,
            Activo = true
        };

        await _conductorRepositorio.AgregarAsync(conductor);
        await _conductorRepositorio.GuardarCambiosAsync();

        await _usuarioRolRepositorio.AgregarAsync(new UsuarioRol
        {
            UsuarioId = usuario.UsuarioId,
            Rol = Rol.CONDUCTOR,
            EmpresaId = null,
            Activo = true
        });
        await _usuarioRolRepositorio.GuardarCambiosAsync();

        var vinculacion = new VinculacionConductorEmpresa
        {
            ConductorId = conductor.ConductorId,
            EmpresaId = empresaId,
            Activa = true
        };

        await _conductorRepositorio.AgregarVinculacionAsync(vinculacion);
        await _conductorRepositorio.GuardarCambiosAsync();

        var vehiculo = new Vehiculo
        {
            ConductorId = conductor.ConductorId,
            Placa = datos.Vehiculo.Placa.Trim(),
            Marca = datos.Vehiculo.Marca,
            Modelo = datos.Vehiculo.Modelo,
            Capacidad = datos.Vehiculo.Capacidad,
            VigenciaSoat = datos.Vehiculo.VigenciaSoat,
            VigenciaTecnomecanica = datos.Vehiculo.VigenciaTecnomecanica,
            Activo = true
        };
        await _vehiculoRepositorio.AgregarAsync(vehiculo);
        await _vehiculoRepositorio.GuardarCambiosAsync();

        await _unidadOperativaRepositorio.AgregarAsync(new UnidadOperativa
        {
            ConductorId = conductor.ConductorId,
            VehiculoId = vehiculo.VehiculoId,
            Activa = true
        });
        await _unidadOperativaRepositorio.GuardarCambiosAsync();

        conductor.Usuario = usuario;
        conductor.VinculacionesConductorEmpresa.Add(vinculacion);

        return ConductorMapper.AConductorDto(conductor);
    }

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
