using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Domain.Entities;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IUsuarioRepositorio"/> mediante Entity Framework
/// Core. Su responsabilidad es exclusivamente la persistencia; no decide
/// reglas de negocio.
/// </summary>
public class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public UsuarioRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<Usuario?> ObtenerPorIdAsync(int usuarioId)
    {
        return await _contexto.Usuarios
            .Include(u => u.UsuarioRoles)
            .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);
    }

    /// <inheritdoc />
    public async Task<Usuario?> ObtenerPorCedulaConRolesAsync(string cedula)
    {
        return await _contexto.Usuarios
            .Include(u => u.UsuarioRoles)
            .FirstOrDefaultAsync(u => u.Cedula == cedula);
    }

    /// <inheritdoc />
    public async Task<Usuario?> ObtenerPorIdentificadorConRolesAsync(string identificador)
    {
        // Los correos se guardan normalizados (ver CorreoNormalizador): se busca con la misma forma.
        var cedula = identificador.Trim();
        var correo = CorreoNormalizador.Normalizar(identificador);
        return await _contexto.Usuarios
            .Include(u => u.UsuarioRoles)
            .FirstOrDefaultAsync(u => u.Cedula == cedula || u.Email == correo);
    }

    /// <inheritdoc />
    public async Task<Usuario?> ObtenerPorEmailAsync(string email)
    {
        var correo = CorreoNormalizador.Normalizar(email);
        return await _contexto.Usuarios.FirstOrDefaultAsync(u => u.Email == correo);
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Usuario usuario)
    {
        await _contexto.Usuarios.AddAsync(usuario);
    }

    /// <inheritdoc />
    public void Descartar(Usuario usuario)
    {
        _contexto.Entry(usuario).State = EntityState.Detached;
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
