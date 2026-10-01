using Microsoft.EntityFrameworkCore;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="ITokenVerificacionRepositorio"/> mediante Entity
/// Framework Core. Su responsabilidad es exclusivamente la persistencia; no
/// decide reglas de negocio.
/// </summary>
public class TokenVerificacionRepositorio : ITokenVerificacionRepositorio
{
    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public TokenVerificacionRepositorio(TransportAppDbContext contexto)
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
