namespace LFMova.Domain.Rules;

/// <summary>
/// Regla de dominio para la alerta de ruta no iniciada (ver §38 de
/// <c>plan.md</c> y <c>tasks.md</c> T094). No decide autorización ni accede a
/// persistencia. La fecha y hora programadas están en hora de Colombia (ver
/// <see cref="ReglasHoraColombia"/>), así que se comparan contra la hora
/// actual de Colombia, nunca contra UTC.
/// </summary>
public static class ReglasAlertaEjecucion
{
    /// <summary>Minutos antes de la hora programada en que empieza a avisarse.</summary>
    public const int MinutosAnticipacion = 60;

    /// <summary>
    /// Minutos después de la hora programada durante los que se sigue
    /// avisando si la ruta no se inició. Es 0 por decisión del usuario: una
    /// vez llega la hora de entrada ese horario ya quedó atrás y no tiene
    /// sentido seguir avisando, así que los avisos se detienen justo a la
    /// hora programada.
    /// </summary>
    public const int MinutosMaximoRetraso = 0;

    /// <summary>
    /// Indica si corresponde alertar: desde <see cref="MinutosAnticipacion"/>
    /// minutos antes de la hora programada y mientras la ruta siga sin
    /// iniciarse, hasta la hora programada (más
    /// <see cref="MinutosMaximoRetraso"/>, que hoy es 0: a la hora de
    /// entrada ya no se avisa). No decide si la ruta ya fue iniciada: quien
    /// invoca esta regla debe filtrar previamente solo los servicios todavía
    /// en <c>PUBLICADO</c>.
    /// </summary>
    public static bool DebeAlertarRutaNoIniciada(DateOnly fecha, TimeOnly horaProgramada, DateTime ahoraColombia)
    {
        var minutos = MinutosParaInicio(fecha, horaProgramada, ahoraColombia);
        return minutos <= MinutosAnticipacion && minutos > -MinutosMaximoRetraso;
    }

    /// <summary>
    /// Minutos que faltan para la hora programada (positivo) o de retraso ya
    /// acumulado (negativo), redondeados hacia arriba (cero es la hora de
    /// inicio).
    /// </summary>
    public static int MinutosParaInicio(DateOnly fecha, TimeOnly horaProgramada, DateTime ahoraColombia)
        => (int)Math.Ceiling((fecha.ToDateTime(horaProgramada) - ahoraColombia).TotalMinutes);
}
