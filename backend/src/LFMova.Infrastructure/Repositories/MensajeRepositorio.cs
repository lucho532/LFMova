using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IMensajeRepositorio"/> mediante Entity Framework
/// Core. Su responsabilidad es exclusivamente la persistencia; no decide
/// reglas de negocio.
/// </summary>
public class MensajeRepositorio : IMensajeRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public MensajeRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<List<Mensaje>> ObtenerPorConversacionAsync(int conversacionId)
    {
        return await _contexto.Mensajes
            .Where(m => m.ConversacionId == conversacionId)
            .OrderBy(m => m.FechaHora)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Mensaje mensaje)
    {
        await _contexto.Mensajes.AddAsync(mensaje);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
