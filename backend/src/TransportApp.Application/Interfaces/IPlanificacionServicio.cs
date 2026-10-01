using TransportApp.Application.DTOs.Planificacion;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define la generación de propuestas de planificación asistida para un
/// servicio (ver <c>spec.md</c> §28, <c>data-model.md</c> §30 y
/// <c>tasks.md</c> Fase 14). Nunca modifica la planificación definitiva: solo
/// produce una recomendación de solo lectura. No decide autorización: eso se
/// verifica en la capa de Api.
/// </summary>
public interface IPlanificacionServicio
{
    /// <summary>
    /// Genera la propuesta de planificación para el servicio indicado,
    /// validando que pertenezca (a través de su jornada) a la empresa
    /// indicada.
    /// </summary>
    Task<PropuestaPlanificacionDto> GenerarPropuestaAsync(int empresaId, int servicioId);
}
