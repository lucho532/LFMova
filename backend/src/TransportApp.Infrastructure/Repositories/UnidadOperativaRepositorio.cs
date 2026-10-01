using Microsoft.EntityFrameworkCore;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IUnidadOperativaRepositorio"/> mediante Entity
/// Framework Core. Su responsabilidad es exclusivamente la persistencia; no
/// decide reglas de negocio.
/// </summary>
public class UnidadOperativaRepositorio : IUnidadOperativaRepositorio
{
    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public UnidadOperativaRepositorio(TransportAppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<UnidadOperativa?> ObtenerPorIdAsync(int unidadOperativaId)
    {
        return await _contexto.UnidadesOperativas.FindAsync(unidadOperativaId);
    }

    /// <inheritdoc />
    public async Task<UnidadOperativa?> ObtenerPorVehiculoAsync(int vehiculoId)
    {
        return await _contexto.UnidadesOperativas.FirstOrDefaultAsync(u => u.VehiculoId == vehiculoId);
    }

    /// <inheritdoc />
    public async Task<List<UnidadOperativa>> ObtenerPorConductorAsync(int conductorId)
    {
        return await _contexto.UnidadesOperativas.Where(u => u.ConductorId == conductorId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(UnidadOperativa unidadOperativa)
    {
        await _contexto.UnidadesOperativas.AddAsync(unidadOperativa);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
