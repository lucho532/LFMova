using Microsoft.EntityFrameworkCore;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IConversacionRepositorio"/> mediante Entity
/// Framework Core. Su responsabilidad es exclusivamente la persistencia; no
/// decide reglas de negocio.
/// </summary>
public class ConversacionRepositorio : IConversacionRepositorio
{
    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public ConversacionRepositorio(TransportAppDbContext contexto)
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
