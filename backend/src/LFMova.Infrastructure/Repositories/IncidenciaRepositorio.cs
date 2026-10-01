using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IIncidenciaRepositorio"/> mediante Entity Framework
/// Core. Su responsabilidad es exclusivamente la persistencia; no decide
/// reglas de negocio.
/// </summary>
public class IncidenciaRepositorio : IIncidenciaRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public IncidenciaRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<Incidencia?> ObtenerPorIdAsync(int incidenciaId)
    {
        return await _contexto.Incidencias.FindAsync(incidenciaId);
    }

    /// <inheritdoc />
    public async Task<List<Incidencia>> ObtenerPorServicioPasajeroAsync(int servicioPasajeroId)
    {
        return await _contexto.Incidencias
            .Where(i => i.ServicioPasajeroId == servicioPasajeroId)
            .OrderBy(i => i.FechaHora)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Incidencia>> ObtenerPorServicioIdAsync(int servicioId)
    {
        return await _contexto.Incidencias
            .Where(i => i.ServicioPasajero!.ServicioId == servicioId)
            .OrderBy(i => i.FechaHora)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Incidencia incidencia)
    {
        await _contexto.Incidencias.AddAsync(incidencia);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
