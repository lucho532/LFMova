using LFMova.Application.DTOs.Llamadas;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define el registro de las llamadas que el conductor hace a un pasajero de
/// un servicio, para que conductor, pasajero y coordinador puedan comprobar
/// después si se llamó y cuándo. No decide quién puede acceder: eso se
/// verifica en la capa de Api.
/// </summary>
public interface IRegistroLlamadaServicio
{
    /// <summary>Deja constancia de que el conductor acaba de pulsar "Llamar" sobre el pasajero.</summary>
    Task<RegistroLlamadaDto> RegistrarAsync(int empresaId, int servicioPasajeroId, RegistrarLlamadaDto datos);

    /// <summary>
    /// Guarda la duración aproximada de una llamada ya registrada. Solo se
    /// guarda una vez y si es verosímil; en otro caso el registro queda sin
    /// duración. Falla si la llamada no es de ese pasajero.
    /// </summary>
    Task<RegistroLlamadaDto> RegistrarDuracionAsync(int empresaId, int servicioPasajeroId, int registroLlamadaId, int segundos);

    /// <summary>Obtiene las llamadas hechas al pasajero indicado.</summary>
    Task<List<RegistroLlamadaDto>> ObtenerPorServicioPasajeroAsync(int empresaId, int servicioPasajeroId);
}
