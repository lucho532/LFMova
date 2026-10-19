using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IZonaRepositorio"/> mediante Entity Framework Core.
/// Su responsabilidad es exclusivamente la persistencia; no decide reglas de
/// negocio.
/// </summary>
public class ZonaRepositorio : IZonaRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public ZonaRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<Zona?> ObtenerPorIdAsync(int zonaId)
    {
        return await _contexto.Zonas.Include(z => z.MacroZona).Include(z => z.CorredorVial).FirstOrDefaultAsync(z => z.ZonaId == zonaId);
    }

    /// <inheritdoc />
    public async Task<List<Zona>> ObtenerPorEmpresaAsync(int empresaId)
    {
        return await _contexto.Zonas.Include(z => z.MacroZona).Include(z => z.CorredorVial).Where(z => z.EmpresaId == empresaId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Zona zona)
    {
        await _contexto.Zonas.AddAsync(zona);
    }

    /// <inheritdoc />
    public void Eliminar(Zona zona)
    {
        _contexto.Zonas.Remove(zona);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
