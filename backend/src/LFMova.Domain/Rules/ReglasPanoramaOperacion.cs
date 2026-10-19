namespace LFMova.Domain.Rules;

/// <summary>
/// Cálculos de los indicadores del panorama de operación del coordinador (variación frente al periodo
/// anterior y promedio de asistencia). No consulta datos ni conoce periodos de calendario.
/// </summary>
public static class ReglasPanoramaOperacion
{
    /// <summary>
    /// Variación porcentual entre dos periodos, con un decimal. Es <c>null</c> cuando el periodo anterior
    /// no tuvo pasajeros, porque no hay base de comparación.
    /// </summary>
    public static double? VariacionPorcentual(int actual, int anterior)
        => anterior <= 0 ? null : Math.Round((actual - anterior) * 100.0 / anterior, 1);

    /// <summary>
    /// Porcentaje de asistencia, con un decimal: asistieron sobre quienes tenían una ruta finalizada
    /// (los que asistieron más los que no; ver <see cref="ReglasAsistenciaRuta"/>). Es 0 si no hay pasajeros.
    /// </summary>
    public static double PromedioAsistencia(int asistieron, int noAsistieron)
    {
        var total = asistieron + noAsistieron;
        return total == 0 ? 0 : Math.Round(asistieron * 100.0 / total, 1);
    }
}
