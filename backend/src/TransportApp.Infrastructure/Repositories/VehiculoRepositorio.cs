using Microsoft.EntityFrameworkCore;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IVehiculoRepositorio"/> mediante Entity Framework
/// Core. Su responsabilidad es exclusivamente la persistencia; no decide
/// reglas de negocio.
/// </summary>
public class VehiculoRepositorio : IVehiculoRepositorio
{
    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public VehiculoRepositorio(TransportAppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<Vehiculo?> ObtenerPorIdAsync(int vehiculoId)
    {
        return await _contexto.Vehiculos.FindAsync(vehiculoId);
    }

    /// <inheritdoc />
    public async Task<List<Vehiculo>> ObtenerPorConductorAsync(int conductorId)
    {
        return await _contexto.Vehiculos.Where(v => v.ConductorId == conductorId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Vehiculo?> ObtenerPorPlacaAsync(string placa)
    {
        return await _contexto.Vehiculos.FirstOrDefaultAsync(v => v.Placa == placa);
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Vehiculo vehiculo)
    {
        await _contexto.Vehiculos.AddAsync(vehiculo);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
