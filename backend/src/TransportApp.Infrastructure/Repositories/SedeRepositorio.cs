using Microsoft.EntityFrameworkCore;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="ISedeRepositorio"/> mediante Entity Framework Core.
/// Su responsabilidad es exclusivamente la persistencia; no decide reglas de
/// negocio.
/// </summary>
public class SedeRepositorio : ISedeRepositorio
{
    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public SedeRepositorio(TransportAppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<Sede?> ObtenerPorIdAsync(int sedeId)
    {
        return await _contexto.Sedes.FindAsync(sedeId);
    }

    /// <inheritdoc />
    public async Task<List<Sede>> ObtenerPorEmpresaAsync(int empresaId)
    {
        return await _contexto.Sedes.Where(s => s.EmpresaId == empresaId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Sede sede)
    {
        await _contexto.Sedes.AddAsync(sede);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
