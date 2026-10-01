using Microsoft.EntityFrameworkCore;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IEmpresaRepositorio"/> mediante Entity Framework
/// Core. Su responsabilidad es exclusivamente la persistencia; no decide
/// reglas de negocio.
/// </summary>
public class EmpresaRepositorio : IEmpresaRepositorio
{
    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public EmpresaRepositorio(TransportAppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<Empresa?> ObtenerPorIdAsync(int empresaId)
    {
        return await _contexto.Empresas.FindAsync(empresaId);
    }

    /// <inheritdoc />
    public async Task<Empresa?> ObtenerPorCifAsync(string cif)
    {
        return await _contexto.Empresas.FirstOrDefaultAsync(e => e.Cif == cif);
    }

    /// <inheritdoc />
    public async Task<List<Empresa>> ObtenerTodasAsync()
    {
        return await _contexto.Empresas.ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Empresa empresa)
    {
        await _contexto.Empresas.AddAsync(empresa);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
