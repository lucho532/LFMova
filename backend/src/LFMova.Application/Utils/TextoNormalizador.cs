using System.Globalization;
using System.Text;

namespace LFMova.Application.Utils;

/// <summary>
/// Lleva un texto escrito por una persona (nombre de sede, barrio) a una forma
/// comparable: sin espacios a los lados, en mayúsculas y sin tildes. Sirve
/// únicamente para comparar textos entre sí; no debe usarse para guardar ni
/// para mostrar el texto resultante.
/// </summary>
public static class TextoNormalizador
{
    /// <summary>Devuelve el texto en mayúsculas, sin espacios a los lados y sin tildes.</summary>
    public static string Normalizar(string texto)
    {
        var descompuesto = texto.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        return new string(descompuesto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }
}
