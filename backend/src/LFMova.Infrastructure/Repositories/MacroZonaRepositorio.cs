using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IMacroZonaRepositorio"/> mediante Entity Framework
/// Core. Su responsabilidad es exclusivamente la persistencia; no decide
/// reglas de negocio.
/// </summary>
public class MacroZonaRepositorio : IMacroZonaRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public MacroZonaRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<MacroZona?> ObtenerPorIdAsync(int macroZonaId)
    {
        return await _contexto.MacroZonas.FindAsync(macroZonaId);
    }

    /// <inheritdoc />
    public async Task<List<MacroZona>> ObtenerPorEmpresaAsync(int empresaId)
    {
        return await _contexto.MacroZonas.Where(m => m.EmpresaId == empresaId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(MacroZona macroZona)
    {
        await _contexto.MacroZonas.AddAsync(macroZona);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
