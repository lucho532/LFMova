using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IConversacionRepositorio"/> mediante Entity
/// Framework Core. Su responsabilidad es exclusivamente la persistencia; no
/// decide reglas de negocio.
/// </summary>
public class ConversacionRepositorio : IConversacionRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public ConversacionRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<Conversacion?> ObtenerPorServicioPasajeroAsync(int servicioPasajeroId)
    {
        return await _contexto.Conversaciones.FirstOrDefaultAsync(c => c.ServicioPasajeroId == servicioPasajeroId);
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Conversacion conversacion)
    {
        await _contexto.Conversaciones.AddAsync(conversacion);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
