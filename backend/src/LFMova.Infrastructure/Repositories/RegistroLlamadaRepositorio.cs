using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IRegistroLlamadaRepositorio"/> mediante Entity
/// Framework Core. Su responsabilidad es exclusivamente la persistencia; no
/// decide reglas de negocio.
/// </summary>
public class RegistroLlamadaRepositorio : IRegistroLlamadaRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public RegistroLlamadaRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<RegistroLlamada?> ObtenerPorIdAsync(int registroLlamadaId)
    {
        return await _contexto.RegistrosLlamada.FindAsync(registroLlamadaId);
    }

    /// <inheritdoc />
    public async Task<List<RegistroLlamada>> ObtenerPorServicioPasajeroAsync(int servicioPasajeroId)
    {
        return await _contexto.RegistrosLlamada
            .Where(r => r.ServicioPasajeroId == servicioPasajeroId)
            .OrderBy(r => r.FechaHora)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(RegistroLlamada registro)
    {
        await _contexto.RegistrosLlamada.AddAsync(registro);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
