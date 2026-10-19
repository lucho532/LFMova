using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="ITokenVerificacionRepositorio"/> mediante Entity
/// Framework Core. Su responsabilidad es exclusivamente la persistencia; no
/// decide reglas de negocio.
/// </summary>
public class TokenVerificacionRepositorio : ITokenVerificacionRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public TokenVerificacionRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<List<TokenVerificacion>> ObtenerNoUtilizadosPorUsuarioAsync(int usuarioId, TipoTokenVerificacion tipo)
    {
        return await _contexto.TokensVerificacion
            .Where(t => t.UsuarioId == usuarioId && t.Tipo == tipo && !t.Utilizado)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(TokenVerificacion token)
    {
        await _contexto.TokensVerificacion.AddAsync(token);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
