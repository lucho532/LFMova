using LFMova.Domain.Entities;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="Conversacion"/> para la capa de aplicación. No decide reglas de
/// negocio ni de autorización.
/// </summary>
public interface IConversacionRepositorio
{
    /// <summary>Obtiene la conversación del pasajero indicado, o <c>null</c> si todavía no existe.</summary>
    Task<Conversacion?> ObtenerPorServicioPasajeroAsync(int servicioPasajeroId);

    /// <summary>Agrega una nueva conversación.</summary>
    Task AgregarAsync(Conversacion conversacion);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
