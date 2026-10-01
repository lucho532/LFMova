using TransportApp.Domain.Entities;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre <see cref="Mensaje"/>
/// para la capa de aplicación. No decide reglas de negocio ni de autorización.
/// </summary>
public interface IMensajeRepositorio
{
    /// <summary>Obtiene los mensajes de la conversación indicada, ordenados por fecha de envío.</summary>
    Task<List<Mensaje>> ObtenerPorConversacionAsync(int conversacionId);

    /// <summary>Agrega un nuevo mensaje.</summary>
    Task AgregarAsync(Mensaje mensaje);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
