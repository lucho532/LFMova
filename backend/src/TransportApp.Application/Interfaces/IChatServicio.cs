using TransportApp.Application.DTOs.Chat;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define la conversación individual entre el conductor y el empleado de un
/// <c>ServicioPasajero</c> (ver <c>spec.md</c> §30/§34 y <c>tasks.md</c>
/// T087). No implementa chat grupal. No decide autorización de acceso al
/// endpoint: eso se verifica en la capa de Api usando
/// <see cref="ObtenerParticipantesAsync"/>.
/// </summary>
public interface IChatServicio
{
    /// <summary>
    /// Obtiene el <c>UsuarioId</c> del empleado y, si tiene unidad asignada,
    /// el del conductor autorizados a participar en la conversación del
    /// pasajero indicado. Devuelve <c>null</c> en cada posición que no pueda
    /// resolverse. Se usa desde la capa de Api para autorizar el acceso.
    /// </summary>
    Task<(int? UsuarioIdEmpleado, int? UsuarioIdConductor)> ObtenerParticipantesAsync(int empresaId, int servicioPasajeroId);

    /// <summary>Obtiene los mensajes de la conversación del pasajero indicado, en orden cronológico.</summary>
    Task<List<MensajeDto>> ObtenerMensajesAsync(int empresaId, int servicioPasajeroId);

    /// <summary>
    /// Envía un mensaje en la conversación del pasajero indicado, creándola
    /// si todavía no existe. Falla si <paramref name="usuarioIdRemitente"/>
    /// no es el empleado ni el conductor autorizados para esa conversación.
    /// </summary>
    Task<MensajeDto> EnviarMensajeAsync(int empresaId, int servicioPasajeroId, int usuarioIdRemitente, EnviarMensajeDto datos);
}
