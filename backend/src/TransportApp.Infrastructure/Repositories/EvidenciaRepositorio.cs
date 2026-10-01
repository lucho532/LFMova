using Microsoft.EntityFrameworkCore;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IEvidenciaRepositorio"/> mediante Entity Framework
/// Core. Su responsabilidad es exclusivamente la persistencia; no decide
/// reglas de negocio.
/// </summary>
public class EvidenciaRepositorio : IEvidenciaRepositorio
{
    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public EvidenciaRepositorio(TransportAppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<List<Evidencia>> ObtenerPorIncidenciaAsync(int incidenciaId)
    {
        return await _contexto.Evidencias
            .Where(e => e.IncidenciaId == incidenciaId)
            .OrderBy(e => e.FechaHora)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Evidencia evidencia)
    {
        await _contexto.Evidencias.AddAsync(evidencia);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
