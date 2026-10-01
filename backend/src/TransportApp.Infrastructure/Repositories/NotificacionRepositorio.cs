using Microsoft.EntityFrameworkCore;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="INotificacionRepositorio"/> mediante Entity
/// Framework Core. Su responsabilidad es exclusivamente la persistencia; no
/// decide reglas de negocio.
/// </summary>
public class NotificacionRepositorio : INotificacionRepositorio
{
    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public NotificacionRepositorio(TransportAppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<List<Notificacion>> ObtenerPorUsuarioAsync(int usuarioId)
    {
        return await _contexto.Notificaciones
            .Where(n => n.UsuarioId == usuarioId)
            .OrderByDescending(n => n.FechaHora)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Notificacion?> ObtenerPorIdAsync(int notificacionId)
    {
        return await _contexto.Notificaciones.FirstOrDefaultAsync(n => n.NotificacionId == notificacionId);
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Notificacion notificacion)
    {
        await _contexto.Notificaciones.AddAsync(notificacion);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
