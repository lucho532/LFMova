using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="INumeroInternoUnidadRepositorio"/> con Entity Framework Core. Solo persiste y
/// consulta; no decide reglas de negocio.
/// </summary>
public class NumeroInternoUnidadRepositorio : INumeroInternoUnidadRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public NumeroInternoUnidadRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public Task<List<NumeroInternoUnidad>> ObtenerPorEmpresaAsync(int empresaId)
        => _contexto.NumerosInternosUnidad.AsNoTracking().Where(n => n.EmpresaId == empresaId).ToListAsync();

    /// <inheritdoc />
    public Task<NumeroInternoUnidad?> ObtenerAsync(int empresaId, int unidadOperativaId)
        => _contexto.NumerosInternosUnidad.FirstOrDefaultAsync(n => n.EmpresaId == empresaId && n.UnidadOperativaId == unidadOperativaId);

    /// <inheritdoc />
    public Task<bool> ExisteEnOtraUnidadAsync(int empresaId, string numero, int exceptoUnidadOperativaId)
    {
        var buscado = numero.ToUpper();
        return _contexto.NumerosInternosUnidad.AnyAsync(n => n.EmpresaId == empresaId && n.UnidadOperativaId != exceptoUnidadOperativaId && n.Numero.ToUpper() == buscado);
    }

    /// <inheritdoc />
    public async Task AgregarAsync(NumeroInternoUnidad numeroInterno) => await _contexto.NumerosInternosUnidad.AddAsync(numeroInterno);

    /// <inheritdoc />
    public void Eliminar(NumeroInternoUnidad numeroInterno) => _contexto.NumerosInternosUnidad.Remove(numeroInterno);

    /// <inheritdoc />
    public Task GuardarCambiosAsync() => _contexto.SaveChangesAsync();
}
