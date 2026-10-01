using Microsoft.EntityFrameworkCore;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="ICorredorVialRepositorio"/> mediante Entity
/// Framework Core. Su responsabilidad es exclusivamente la persistencia; no
/// decide reglas de negocio.
/// </summary>
public class CorredorVialRepositorio : ICorredorVialRepositorio
{
    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public CorredorVialRepositorio(TransportAppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<CorredorVial?> ObtenerPorIdAsync(int corredorVialId)
    {
        return await _contexto.CorredoresViales.FindAsync(corredorVialId);
    }

    /// <inheritdoc />
    public async Task<List<CorredorVial>> ObtenerPorEmpresaAsync(int empresaId)
    {
        return await _contexto.CorredoresViales.Where(c => c.EmpresaId == empresaId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(CorredorVial corredorVial)
    {
        await _contexto.CorredoresViales.AddAsync(corredorVial);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
