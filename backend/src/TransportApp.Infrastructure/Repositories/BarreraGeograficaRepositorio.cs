using Microsoft.EntityFrameworkCore;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IBarreraGeograficaRepositorio"/> mediante Entity
/// Framework Core. Su responsabilidad es exclusivamente la persistencia; no
/// decide reglas de negocio.
/// </summary>
public class BarreraGeograficaRepositorio : IBarreraGeograficaRepositorio
{
    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public BarreraGeograficaRepositorio(TransportAppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<BarreraGeografica?> ObtenerPorIdAsync(int barreraGeograficaId)
    {
        return await _contexto.BarrerasGeograficas.FindAsync(barreraGeograficaId);
    }

    /// <inheritdoc />
    public async Task<List<BarreraGeografica>> ObtenerPorEmpresaAsync(int empresaId)
    {
        return await _contexto.BarrerasGeograficas.Where(b => b.EmpresaId == empresaId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(BarreraGeografica barrera)
    {
        await _contexto.BarrerasGeograficas.AddAsync(barrera);
    }

    /// <inheritdoc />
    public void Eliminar(BarreraGeografica barrera)
    {
        _contexto.BarrerasGeograficas.Remove(barrera);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
