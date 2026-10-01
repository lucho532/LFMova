namespace TransportApp.Application.DTOs.Planificacion;

/// <summary>
/// Representa la propuesta de planificación generada para un servicio (ver
/// <c>tasks.md</c> T076/T081). Es una recomendación de solo lectura: nunca
/// modifica automáticamente la planificación definitiva. El coordinador
/// conserva la decisión final y puede aceptarla, modificarla o rechazarla
/// mediante las operaciones manuales ya existentes (asignar unidad, reordenar
/// pasajeros, cambiar estado).
/// </summary>
public class PropuestaPlanificacionDto
{
    /// <summary>Servicio para el que se generó la propuesta.</summary>
    public int ServicioId { get; set; }

    /// <summary>
    /// Orden propuesto de los pasajeros del servicio. Para servicios de
    /// <c>ENTRADA</c> sigue la heurística de más lejano a más cercano de la
    /// sede (ver <c>tasks.md</c> T079); para <c>SALIDA</c> conserva el orden
    /// ya registrado, porque el SDD no define una heurística para ese caso.
    /// </summary>
    public List<PasajeroPropuestoDto> OrdenPropuesto { get; set; } = new();

    /// <summary>
    /// Hora límite de llegada a la sede (al menos 15 minutos antes de la hora
    /// programada). Solo aplica a servicios de <c>ENTRADA</c>.
    /// </summary>
    public TimeOnly? HoraLimiteLlegadaSede { get; set; }

    /// <summary>
    /// Hora sugerida de inicio de recogida, considerando la ventana conjunta
    /// aproximada de recogida (ver <c>tasks.md</c> T080). Solo aplica a
    /// servicios de <c>ENTRADA</c>.
    /// </summary>
    public TimeOnly? HoraSugeridaInicioRecogida { get; set; }

    /// <summary>
    /// Distancia en línea recta entre el punto final del servicio anterior de
    /// la misma unidad operativa (mismo día) y el primer punto de este
    /// servicio, en kilómetros. <c>null</c> si no hay servicio anterior o
    /// faltan coordenadas (ver <c>tasks.md</c> T078).
    /// </summary>
    public double? DistanciaDesdeServicioAnteriorKm { get; set; }

    /// <summary>
    /// Distancia en línea recta entre el punto final de este servicio y el
    /// primer punto del siguiente servicio de la misma unidad operativa
    /// (mismo día), en kilómetros. <c>null</c> si no hay servicio siguiente o
    /// faltan coordenadas (ver <c>tasks.md</c> T078).
    /// </summary>
    public double? DistanciaHaciaServicioSiguienteKm { get; set; }

    /// <summary>
    /// Advertencias informativas de la propuesta (por ejemplo, capacidad
    /// excedida o coordenadas faltantes). Nunca bloquean una decisión manual
    /// del coordinador.
    /// </summary>
    public List<string> Advertencias { get; set; } = new();
}
