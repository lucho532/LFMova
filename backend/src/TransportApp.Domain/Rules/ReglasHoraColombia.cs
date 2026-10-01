namespace TransportApp.Domain.Rules;

/// <summary>
/// Hora de referencia de la operación: la de Colombia (ver <c>AGENTS.md</c>
/// §41). Las fechas y horas programadas (<c>Servicio.Fecha</c>,
/// <c>Servicio.HoraProgramada</c>, <c>Jornada.FechaOperativa</c>,
/// <c>ProgramacionTransporte</c>) se guardan tal como las escribe el
/// coordinador, en hora de Colombia; por eso, para compararlas con "ahora"
/// hay que llevar el instante actual (UTC) a hora de Colombia. Colombia usa
/// UTC−5 todo el año, sin horario de verano, así que basta un desfase fijo
/// (sin <c>TimeZoneInfo</c>). No decide qué hacer con la comparación.
/// </summary>
public static class ReglasHoraColombia
{
    /// <summary>Diferencia fija de Colombia respecto a UTC.</summary>
    public static readonly TimeSpan DesfaseUtc = TimeSpan.FromHours(-5);

    /// <summary>Convierte un instante UTC en la fecha y hora de reloj de Colombia.</summary>
    public static DateTime AhoraColombia(DateTime ahoraUtc)
        => DateTime.SpecifyKind(ahoraUtc.Add(DesfaseUtc), DateTimeKind.Unspecified);
}
