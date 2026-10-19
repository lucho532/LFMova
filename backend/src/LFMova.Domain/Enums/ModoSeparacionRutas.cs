namespace LFMova.Domain.Enums;

/// <summary>
/// Forma en que un Excel con rutas ya armadas indica dónde empieza cada ruta. En ambos modos la
/// plataforma respeta las rutas tal como vienen: no las divide, no las reordena ni mueve pasajeros.
/// </summary>
public enum ModoSeparacionRutas
{
    /// <summary>Cada fila trae el número de su ruta en una columna (por ejemplo "No RUTA"); las filas con el mismo número forman una ruta.</summary>
    COLUMNA_NUMERO_RUTA,

    /// <summary>Una línea de texto que empieza con el texto indicado por el coordinador abre cada ruta; las filas siguientes son sus pasajeros.</summary>
    LINEA_INICIO
}
