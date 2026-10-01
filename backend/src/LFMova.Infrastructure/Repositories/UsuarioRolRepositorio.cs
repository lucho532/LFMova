using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IUsuarioRolRepositorio"/> mediante Entity Framework
/// Core. Su responsabilidad es exclusivamente la persistencia; no decide
/// reglas de negocio.
/// </summary>
public class UsuarioRolRepositorio : IUsuarioRolRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public UsuarioRolRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<UsuarioRol?> ObtenerPorIdAsync(int usuarioRolId)
    {
        return await _contexto.UsuarioRoles
            .Include(ur => ur.Usuario)
            .FirstOrDefaultAsync(ur => ur.UsuarioRolId == usuarioRolId);
    }

    /// <inheritdoc />
    public async Task<List<UsuarioRol>> ObtenerPorEmpresaYRolAsync(int empresaId, Rol rol)
    {
        return await _contexto.UsuarioRoles
            .Include(ur => ur.Usuario)
            .Where(ur => ur.EmpresaId == empresaId && ur.Rol == rol)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(UsuarioRol usuarioRol)
    {
        await _contexto.UsuarioRoles.AddAsync(usuarioRol);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
