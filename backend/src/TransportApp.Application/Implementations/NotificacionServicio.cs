using TransportApp.Application.DTOs.Notificaciones;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Mappers;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa la creación, consulta y marcado como leída de notificaciones
/// de usuario (ver <c>data-model.md</c> §23 y <c>tasks.md</c> T075). No
/// decide a quién debe notificarse ni con qué contenido; eso corresponde al
/// caso de uso que invoca <see cref="CrearAsync"/>.
/// </summary>
public class NotificacionServicio : INotificacionServicio
{
    private readonly INotificacionRepositorio _notificacionRepositorio;

    /// <summary>Crea el servicio con su repositorio.</summary>
    public NotificacionServicio(INotificacionRepositorio notificacionRepositorio)
    {
        _notificacionRepositorio = notificacionRepositorio;
    }

    /// <inheritdoc />
    public async Task CrearAsync(int usuarioId, string tipo, string titulo, string mensaje, string? enlace = null)
    {
        await _notificacionRepositorio.AgregarAsync(new Notificacion
        {
            UsuarioId = usuarioId,
            Tipo = tipo,
            Titulo = titulo,
            Mensaje = mensaje,
            Enlace = enlace,
            Leida = false,
            FechaHora = DateTime.UtcNow
        });

        await _notificacionRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task<List<NotificacionDto>> ObtenerPorUsuarioAsync(int usuarioId)
    {
        var notificaciones = await _notificacionRepositorio.ObtenerPorUsuarioAsync(usuarioId);
        return notificaciones.Select(NotificacionMapper.ANotificacionDto).ToList();
    }

    /// <inheritdoc />
    public async Task MarcarComoLeidaAsync(int usuarioId, int notificacionId)
    {
        var notificacion = await _notificacionRepositorio.ObtenerPorIdAsync(notificacionId);
        if (notificacion is null || notificacion.UsuarioId != usuarioId)
        {
            throw new InvalidOperationException("La notificación indicada no existe para este usuario.");
        }

        notificacion.Leida = true;
        await _notificacionRepositorio.GuardarCambiosAsync();
    }
}
