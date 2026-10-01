using LFMova.Domain.Entities;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="Notificacion"/> para la capa de aplicación. No decide reglas de
/// negocio ni de autorización; esas decisiones corresponden a
/// <c>INotificacionServicio</c> (ver <c>tasks.md</c> T075).
/// </summary>
public interface INotificacionRepositorio
{
    /// <summary>Obtiene las notificaciones del usuario indicado, más recientes primero.</summary>
    Task<List<Notificacion>> ObtenerPorUsuarioAsync(int usuarioId);

    /// <summary>Obtiene una notificación por su identificador.</summary>
    Task<Notificacion?> ObtenerPorIdAsync(int notificacionId);

    /// <summary>Agrega una nueva notificación.</summary>
    Task AgregarAsync(Notificacion notificacion);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
