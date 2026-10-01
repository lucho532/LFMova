using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LFMova.Domain.Rules;

/// <summary>
/// Nomenclatura urbana de Villamaría (Caldas), para distinguir barrios que se
/// llaman igual en Villamaría y en Manizales (por ejemplo, "Nuevo
/// Horizonte"): si una dirección pide un cruce de calle y carrera que no
/// existe en Villamaría, esa persona no puede vivir allí. Los rangos salen de
/// OpenStreetMap (consulta del 30/09/2026 sobre la "Cabecera Municipal
/// Villamaría"): el casco urbano usa calles 1 a 22 y carreras 1 a 20; un
/// sector oriental, en el límite con Manizales, continúa la numeración de
/// Manizales (calles 43 a 71, carreras 36 y 71). Si Villamaría abre vías
/// nuevas fuera de estos rangos, hay que actualizarlos aquí. No decide a qué
/// zona va nadie: solo dice si una dirección es posible en Villamaría.
/// </summary>
public static class ReglasNomenclaturaVillamaria
{
    /// <summary>Sectores de Villamaría: en cada uno, las calles y carreras que existen (y se cruzan entre sí).</summary>
    private static readonly (int CalleDesde, int CalleHasta, int CarreraDesde, int CarreraHasta)[] Sectores =
    {
        (1, 22, 1, 20),   // casco urbano
        (40, 75, 30, 75), // sector oriental, con la numeración de Manizales
    };

    /// <summary>
    /// "Calle 10 A1 # 38 A-20", "CRA 5 N° 12-30", "Cl 8 #15-2": tipo de vía, su número y el número de la
    /// vía que la cruza (el primero después de "#" o "N°").
    /// </summary>
    private static readonly Regex Direccion = new(
        @"^\s*(?<tipo>CALLE|CLL|CL|CARRERA|CRA|KRA|KR|CR)\.?\s*(?<via>\d+)[^#]*?(?:#|N\s*[O°º]\.?|NO\.?)\s*(?<cruce>\d+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Indica si la dirección puede ser de Villamaría: <c>true</c> si su cruce
    /// de calle y carrera existe en algún sector de Villamaría, <c>false</c>
    /// si no existe en ninguno (la persona no vive allí) y <c>null</c> si la
    /// dirección no tiene el formato "Calle/Carrera N # M" y no se puede saber.
    /// </summary>
    public static bool? DireccionPuedeSerDeVillamaria(string? direccion)
    {
        if (string.IsNullOrWhiteSpace(direccion))
        {
            return null;
        }

        var coincidencia = Direccion.Match(SinTildes(direccion.ToUpperInvariant()));
        if (!coincidencia.Success)
        {
            return null;
        }

        var via = int.Parse(coincidencia.Groups["via"].Value, CultureInfo.InvariantCulture);
        var cruce = int.Parse(coincidencia.Groups["cruce"].Value, CultureInfo.InvariantCulture);
        var esCalle = coincidencia.Groups["tipo"].Value is "CALLE" or "CLL" or "CL";
        var (calle, carrera) = esCalle ? (via, cruce) : (cruce, via);

        return Sectores.Any(s => calle >= s.CalleDesde && calle <= s.CalleHasta && carrera >= s.CarreraDesde && carrera <= s.CarreraHasta);
    }

    private static string SinTildes(string texto)
        => new(texto.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
}
