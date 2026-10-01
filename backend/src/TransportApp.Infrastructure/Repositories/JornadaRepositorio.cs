using Microsoft.EntityFrameworkCore;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IJornadaRepositorio"/> mediante Entity Framework
/// Core. Su responsabilidad es exclusivamente la persistencia; no decide
/// reglas de negocio.
/// </summary>
public class JornadaRepositorio : IJornadaRepositorio
{
    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public JornadaRepositorio(TransportAppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<Jornada?> ObtenerPorIdAsync(int jornadaId)
    {
        return await _contexto.Jornadas.FindAsync(jornadaId);
    }

    /// <inheritdoc />
    public async Task<List<Jornada>> ObtenerPorEmpresaAsync(int empresaId)
    {
        return await _contexto.Jornadas.Where(j => j.EmpresaId == empresaId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Jornada jornada)
    {
        await _contexto.Jornadas.AddAsync(jornada);
    }

    /// <inheritdoc />
    public Task EliminarAsync(Jornada jornada)
    {
        _contexto.Jornadas.Remove(jornada);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
