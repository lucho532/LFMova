namespace LFMova.Domain.Rules;

/// <summary>
/// Reglas del cobro mensual por conductor (decisión del 2026-10-05): se
/// factura por cada conductor que finalizó al menos una ruta en el mes,
/// contado por el día de finalización en hora de Colombia. No accede a
/// persistencia ni calcula valores en dinero.
/// </summary>
public static class ReglasFacturacion
{
    /// <summary>Indica si el año y el mes forman un mes válido.</summary>
    public static bool EsMesValido(int anio, int mes) => anio is >= 2000 and <= 2999 && mes is >= 1 and <= 12;

    /// <summary>Primer y último día del mes indicado.</summary>
    public static (DateOnly Desde, DateOnly Hasta) RangoDelMes(int anio, int mes)
    {
        var desde = new DateOnly(anio, mes, 1);
        return (desde, desde.AddMonths(1).AddDays(-1));
    }

    /// <summary>
    /// Indica si el mes ya terminó en Colombia. Solo un mes terminado se
    /// puede cerrar: mientras corre, todavía pueden finalizarse rutas.
    /// </summary>
    public static bool MesTerminado(int anio, int mes, DateTime ahoraUtc)
        => RangoDelMes(anio, mes).Hasta < DiaColombia(ahoraUtc);

    /// <summary>Día de Colombia que corresponde a un instante UTC (por ejemplo, la hora real de fin de una ruta).</summary>
    public static DateOnly DiaColombia(DateTime instanteUtc) => DateOnly.FromDateTime(ReglasHoraColombia.AhoraColombia(instanteUtc));
}
