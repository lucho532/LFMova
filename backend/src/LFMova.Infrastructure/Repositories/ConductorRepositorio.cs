using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IConductorRepositorio"/> mediante Entity Framework
/// Core. Su responsabilidad es exclusivamente la persistencia; no decide
/// reglas de negocio.
/// </summary>
public class ConductorRepositorio : IConductorRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public ConductorRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<Conductor?> ObtenerPorIdAsync(int conductorId)
    {
        return await _contexto.Conductores
            .Include(c => c.Usuario)
            .Include(c => c.VinculacionesConductorEmpresa)
            .FirstOrDefaultAsync(c => c.ConductorId == conductorId);
    }

    /// <inheritdoc />
    public async Task<Conductor?> ObtenerPorUsuarioIdAsync(int usuarioId)
    {
        return await _contexto.Conductores
            .Include(c => c.Usuario)
            .Include(c => c.VinculacionesConductorEmpresa)
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
    }

    /// <inheritdoc />
    public async Task<List<Conductor>> ObtenerPorEmpresaAsync(int empresaId)
    {
        return await _contexto.Conductores
            .Include(c => c.Usuario)
            .Include(c => c.VinculacionesConductorEmpresa)
            .Where(c => c.VinculacionesConductorEmpresa.Any(v => v.EmpresaId == empresaId))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Conductor conductor)
    {
        await _contexto.Conductores.AddAsync(conductor);
    }

    /// <inheritdoc />
    public async Task AgregarVinculacionAsync(VinculacionConductorEmpresa vinculacion)
    {
        await _contexto.VinculacionesConductorEmpresa.AddAsync(vinculacion);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
