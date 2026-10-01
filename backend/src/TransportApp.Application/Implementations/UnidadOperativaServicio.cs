using TransportApp.Application.DTOs.UnidadesOperativas;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Mappers;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Rules;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de creación y gestión de unidades operativas
/// (conductor + vehículo). Aplica <see cref="ReglasUnidadOperativa"/> para
/// garantizar que el vehículo pertenezca al mismo conductor. No decide
/// autorización: eso se verifica en la capa de Api.
/// </summary>
public class UnidadOperativaServicio : IUnidadOperativaServicio
{
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IVehiculoRepositorio _vehiculoRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;

    /// <summary>Crea el servicio con sus repositorios.</summary>
    public UnidadOperativaServicio(
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IVehiculoRepositorio vehiculoRepositorio,
        IConductorRepositorio conductorRepositorio)
    {
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _vehiculoRepositorio = vehiculoRepositorio;
        _conductorRepositorio = conductorRepositorio;
    }

    /// <inheritdoc />
    public async Task<UnidadOperativaDto> CrearAsync(int conductorId, CrearUnidadOperativaDto datos)
    {
        var conductor = await _conductorRepositorio.ObtenerPorIdAsync(conductorId);
        if (conductor is null)
        {
            throw new InvalidOperationException("El conductor indicado no existe.");
        }

        var vehiculo = await _vehiculoRepositorio.ObtenerPorIdAsync(datos.VehiculoId);
        if (vehiculo is null)
        {
            throw new InvalidOperationException("El vehículo indicado no existe.");
        }

        var unidadOperativa = new UnidadOperativa
        {
            ConductorId = conductorId,
            VehiculoId = datos.VehiculoId,
            Activa = true
        };

        if (!ReglasUnidadOperativa.EsConsistente(unidadOperativa, vehiculo))
        {
            throw new InvalidOperationException("El vehículo no pertenece a este conductor.");
        }

        var unidadExistente = await _unidadOperativaRepositorio.ObtenerPorVehiculoAsync(datos.VehiculoId);
        if (unidadExistente is not null)
        {
            throw new InvalidOperationException("El vehículo ya tiene una unidad operativa registrada.");
        }

        await _unidadOperativaRepositorio.AgregarAsync(unidadOperativa);
        await _unidadOperativaRepositorio.GuardarCambiosAsync();

        return UnidadOperativaMapper.AUnidadOperativaDto(unidadOperativa);
    }

    /// <inheritdoc />
    public async Task<List<UnidadOperativaDto>> ObtenerPorConductorAsync(int conductorId)
    {
        var unidades = await _unidadOperativaRepositorio.ObtenerPorConductorAsync(conductorId);
        return unidades.Select(UnidadOperativaMapper.AUnidadOperativaDto).ToList();
    }

    /// <inheritdoc />
    public async Task ActivarAsync(int conductorId, int unidadOperativaId)
    {
        var unidadOperativa = await ObtenerUnidadDelConductorOFallarAsync(conductorId, unidadOperativaId);
        unidadOperativa.Activa = true;
        await _unidadOperativaRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task DesactivarAsync(int conductorId, int unidadOperativaId)
    {
        var unidadOperativa = await ObtenerUnidadDelConductorOFallarAsync(conductorId, unidadOperativaId);
        unidadOperativa.Activa = false;
        await _unidadOperativaRepositorio.GuardarCambiosAsync();
    }

    private async Task<UnidadOperativa> ObtenerUnidadDelConductorOFallarAsync(int conductorId, int unidadOperativaId)
    {
        var unidadOperativa = await _unidadOperativaRepositorio.ObtenerPorIdAsync(unidadOperativaId);
        if (unidadOperativa is null || unidadOperativa.ConductorId != conductorId)
        {
            throw new InvalidOperationException("La unidad operativa indicada no existe para este conductor.");
        }

        return unidadOperativa;
    }
}
