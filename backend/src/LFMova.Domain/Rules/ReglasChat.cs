namespace LFMova.Domain.Rules;

/// <summary>
/// Reglas de quién puede escribir en la conversación de un pasajero y qué. El chat es de un solo
/// sentido para evitar distracciones: el conductor solo envía mensajes predeterminados y el pasajero
/// únicamente los lee. No persiste ni consulta datos.
/// </summary>
public static class ReglasChat
{
    /// <summary>
    /// Mensajes que el conductor puede enviar al pasajero con un toque. Deben coincidir con
    /// <c>MENSAJES_RAPIDOS_CONDUCTOR</c> del frontend (<c>RespuestasRapidas.tsx</c>).
    /// </summary>
    public static readonly IReadOnlyList<string> MensajesPredeterminadosConductor =
    [
        "Estoy en camino",
        "Voy un poco tarde",
        "Estoy llegando",
        "Puedes salir",
        "He llegado",
        "Gracias",
    ];

    /// <summary>Indica si el contenido es exactamente uno de los mensajes predeterminados del conductor.</summary>
    public static bool EsMensajePredeterminado(string? contenido)
    {
        return contenido is not null && MensajesPredeterminadosConductor.Contains(contenido.Trim());
    }
}
