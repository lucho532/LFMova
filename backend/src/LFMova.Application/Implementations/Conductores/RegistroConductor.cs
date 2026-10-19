using LFMova.Application.DTOs.Conductores;
using LFMova.Application.Interfaces;
using LFMova.Application.Mappers;
using LFMova.Application.Validators;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.Application.Implementations.Conductores;

/// <summary>
/// Convierte en conductor a una persona ya registrada, buscada por su cédula: crea su perfil de
/// conductor, el rol <c>CONDUCTOR</c>, la vinculación con la empresa, su vehículo y su unidad
/// operativa. Nunca crea una cuenta de usuario ni un segundo conductor para la misma persona (ver
/// <c>AGENTS.md</c> §14 y §16). No decide autorización.
/// </summary>
public class RegistroConductor
{
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IEmpresaRepositorio _empresaRepositorio;
    private readonly IUsuarioRolRepositorio _usuarioRolRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IVehiculoRepositorio _vehiculoRepositorio;

    /// <summary>Crea el colaborador con sus repositorios.</summary>
    public RegistroConductor(
        IConductorRepositorio conductorRepositorio,
        IUsuarioRepositorio usuarioRepositorio,
        IEmpresaRepositorio empresaRepositorio,
        IUsuarioRolRepositorio usuarioRolRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IVehiculoRepositorio vehiculoRepositorio)
    {
        _conductorRepositorio = conductorRepositorio;
        _usuarioRepositorio = usuarioRepositorio;
        _empresaRepositorio = empresaRepositorio;
        _usuarioRolRepositorio = usuarioRolRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _vehiculoRepositorio = vehiculoRepositorio;
    }

    /// <summary>Registra como conductor de la empresa a la persona de esa cédula, con su vehículo y su unidad operativa.</summary>
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
}
