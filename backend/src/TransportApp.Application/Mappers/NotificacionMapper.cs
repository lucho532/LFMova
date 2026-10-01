using TransportApp.Application.DTOs.Notificaciones;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="Notificacion"/> y su DTO. No contiene reglas de
/// negocio ni realiza consultas de persistencia.
/// </summary>
public static class NotificacionMapper
{
    /// <summary>Convierte una entidad <see cref="Notificacion"/> en su DTO público.</summary>
    public static NotificacionDto ANotificacionDto(Notificacion notificacion) => new()
    {
        NotificacionId = notificacion.NotificacionId,
        Tipo = notificacion.Tipo,
        Titulo = notificacion.Titulo,
        Mensaje = notificacion.Mensaje,
        Enlace = notificacion.Enlace,
        Leida = notificacion.Leida,
        FechaHora = notificacion.FechaHora
    };
}
