using TransportApp.Application.DTOs.Notificaciones;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Formaliza la creación y gestión de notificaciones de usuario (ver
/// <c>tasks.md</c> T075). Es el punto único que otros casos de uso de
/// aplicación deben utilizar para generar notificaciones; no decide
/// autorización de acceso a los endpoints que lo exponen, eso se verifica en
/// la capa de Api.
/// </summary>
public interface INotificacionServicio
{
    /// <summary>Crea una notificación no leída para el usuario indicado.</summary>
    Task CrearAsync(int usuarioId, string tipo, string titulo, string mensaje, string? enlace = null);

    /// <summary>Consulta las notificaciones del usuario indicado, más recientes primero.</summary>
    Task<List<NotificacionDto>> ObtenerPorUsuarioAsync(int usuarioId);

    /// <summary>
    /// Marca como leída una notificación del usuario indicado. Falla si la
    /// notificación no existe o no pertenece a ese usuario.
    /// </summary>
    Task MarcarComoLeidaAsync(int usuarioId, int notificacionId);
}
