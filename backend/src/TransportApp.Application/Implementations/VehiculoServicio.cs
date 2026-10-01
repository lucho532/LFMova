using TransportApp.Application.DTOs.Vehiculos;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Mappers;
using TransportApp.Application.Validators;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de registro y gestión de vehículos. Un
/// vehículo pertenece exclusivamente al conductor que lo registra. No decide
/// autorización: eso se verifica en la capa de Api.
/// </summary>
public class VehiculoServicio : IVehiculoServicio
{
    private readonly IVehiculoRepositorio _vehiculoRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;

    /// <summary>Crea el servicio con sus repositorios.</summary>
    public VehiculoServicio(
        IVehiculoRepositorio vehiculoRepositorio,
        IConductorRepositorio conductorRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio)
    {
        _vehiculoRepositorio = vehiculoRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
    }

    /// <inheritdoc />
    public async Task<VehiculoDto> CrearAsync(int conductorId, CrearVehiculoDto datos)
    {
        var conductor = await _conductorRepositorio.ObtenerPorIdAsync(conductorId);
        if (conductor is null)
        {
            throw new InvalidOperationException("El conductor indicado no existe.");
        }

        var errorDeDatos = VehiculoValidador.ObtenerError(datos);
        if (errorDeDatos is not null)
        {
            throw new InvalidOperationException(errorDeDatos);
        }

        if (await _vehiculoRepositorio.ObtenerPorPlacaAsync(datos.Placa.Trim()) is not null)
        {
            throw new InvalidOperationException("Ya existe un vehículo registrado con esta placa.");
        }

        var vehiculo = new Vehiculo
        {
            ConductorId = conductorId,
            Placa = datos.Placa.Trim(),
            Marca = datos.Marca,
            Modelo = datos.Modelo,
            Capacidad = datos.Capacidad,
            VigenciaSoat = datos.VigenciaSoat,
            VigenciaTecnomecanica = datos.VigenciaTecnomecanica,
            Activo = true
        };

        await _vehiculoRepositorio.AgregarAsync(vehiculo);
        await _vehiculoRepositorio.GuardarCambiosAsync();

        return VehiculoMapper.AVehiculoDto(vehiculo);
    }

    /// <inheritdoc />
    public async Task<VehiculoDto> ActualizarAsync(int conductorId, int vehiculoId, CrearVehiculoDto datos)
    {
        var vehiculo = await _vehiculoRepositorio.ObtenerPorIdAsync(vehiculoId);
        if (vehiculo is null || vehiculo.ConductorId != conductorId)
        {
            throw new InvalidOperationException("El vehículo indicado no existe para este conductor.");
        }

        var errorDeDatos = VehiculoValidador.ObtenerError(datos);
        if (errorDeDatos is not null)
        {
            throw new InvalidOperationException(errorDeDatos);
        }

        var otroConLaPlaca = await _vehiculoRepositorio.ObtenerPorPlacaAsync(datos.Placa.Trim());
        if (otroConLaPlaca is not null && otroConLaPlaca.VehiculoId != vehiculoId)
        {
            throw new InvalidOperationException("Ya existe un vehículo registrado con esta placa.");
        }

        vehiculo.Placa = datos.Placa.Trim();
        vehiculo.Marca = datos.Marca;
        vehiculo.Modelo = datos.Modelo;
        vehiculo.Capacidad = datos.Capacidad;
        vehiculo.VigenciaSoat = datos.VigenciaSoat;
        vehiculo.VigenciaTecnomecanica = datos.VigenciaTecnomecanica;
        await _vehiculoRepositorio.GuardarCambiosAsync();

        return VehiculoMapper.AVehiculoDto(vehiculo);
    }

    /// <inheritdoc />
    public async Task<VehiculoDto?> ObtenerPorIdAsync(int vehiculoId)
    {
        var vehiculo = await _vehiculoRepositorio.ObtenerPorIdAsync(vehiculoId);
        return vehiculo is null ? null : VehiculoMapper.AVehiculoDto(vehiculo);
    }

    /// <inheritdoc />
    public async Task<List<VehiculoDto>> ObtenerPorConductorAsync(int conductorId)
    {
        var vehiculos = await _vehiculoRepositorio.ObtenerPorConductorAsync(conductorId);
        return vehiculos.Select(VehiculoMapper.AVehiculoDto).ToList();
    }

    /// <inheritdoc />
    public async Task ActivarAsync(int conductorId, int vehiculoId)
    {
        var vehiculo = await ObtenerVehiculoDelConductorOFallarAsync(conductorId, vehiculoId);
        vehiculo.Activo = true;
        await _vehiculoRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task DesactivarAsync(int conductorId, int vehiculoId)
    {
        var vehiculo = await ObtenerVehiculoDelConductorOFallarAsync(conductorId, vehiculoId);
        vehiculo.Activo = false;

        var unidadOperativa = await _unidadOperativaRepositorio.ObtenerPorVehiculoAsync(vehiculoId);
        if (unidadOperativa is not null && unidadOperativa.Activa)
        {
            unidadOperativa.Activa = false;
        }

        await _vehiculoRepositorio.GuardarCambiosAsync();
    }

    private async Task<Vehiculo> ObtenerVehiculoDelConductorOFallarAsync(int conductorId, int vehiculoId)
    {
        var vehiculo = await _vehiculoRepositorio.ObtenerPorIdAsync(vehiculoId);
        if (vehiculo is null || vehiculo.ConductorId != conductorId)
        {
            throw new InvalidOperationException("El vehículo indicado no existe para este conductor.");
        }

        return vehiculo;
    }
}
