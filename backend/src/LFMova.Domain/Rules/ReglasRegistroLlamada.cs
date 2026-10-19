namespace LFMova.Domain.Rules;

/// <summary>
/// Reglas sobre la duración aproximada de una llamada registrada. La duración
/// la mide el dispositivo del conductor (tiempo fuera de la aplicación), así
/// que solo se acepta si es verosímil. No persiste ni consulta datos.
/// </summary>
public static class ReglasRegistroLlamada
{
    /// <summary>
    /// Tope de una duración creíble. Si el conductor tarda más en volver a
    /// la aplicación, lo más probable es que la dejara en segundo plano, no
    /// que siguiera hablando: la llamada es para avisar que llegó.
    /// </summary>
    public const int DuracionMaximaSegundos = 30 * 60;

    /// <summary>Indica si los segundos medidos pueden guardarse como duración de la llamada.</summary>
    public static bool EsDuracionVerosimil(int segundos)
    {
        return segundos > 0 && segundos <= DuracionMaximaSegundos;
    }
}
