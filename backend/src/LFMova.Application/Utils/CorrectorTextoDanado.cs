using System.Text;
using System.Text.RegularExpressions;

namespace LFMova.Application.Utils;

/// <summary>
/// Intenta recuperar los textos de un Excel cuya codificación se dañó (por ejemplo "Casta�o" por
/// "Castaño" o "Rodr�guez" por "Rodríguez"): primero deshace la doble codificación y, si quedan
/// caracteres de reemplazo, los resuelve con reglas del español y una lista de apellidos y palabras
/// comunes con tilde. Si no puede resolver un carácter lo deja y lo informa. No valida ni cambia
/// textos sanos.
/// </summary>
public static class CorrectorTextoDanado
{
    private const char Reemplazo = '�';
    private const string Vocales = "aeiouáéíóúAEIOUÁÉÍÓÚ";
    private const string VocalesConTilde = "áéíóú";

    private static readonly Regex DobleCodificacion = new("[ÃÂ][\u0080-¿]", RegexOptions.Compiled);

    /// <summary>Palabras con tilde frecuentes en nombres, apellidos y direcciones, para decidir qué vocal falta.</summary>
    private static readonly Dictionary<string, string> Palabras = (
        "Pérez Gómez González Rodríguez Martínez Hernández Jiménez Ramírez Sánchez López Díaz Álvarez Vásquez Márquez Suárez Cárdenas Ríos " +
        "Bermúdez Benítez Domínguez Fernández Gutiérrez García María José Andrés Adrián Darío Fabián Germán Iván Julián Sebastián Tomás " +
        "Jesús Ángel Ángela Óscar Mónica Verónica Tobón Úsuga Hincapié Góngora Rúa Peláez Velásquez Vélez Mejía Rincón Ramón Simón Sofía " +
        "Lucía Rocío Raúl Aníbal Héctor Víctor Cristóbal Nicolás Agustín Martín Joaquín Benjamín Efraín Aristizábal Berrío Marín " +
        "Cortés Galvéz Ruíz Muñiz Ortíz Arbeláez Echavarría Tamayo Cañas Giraldo Valderrama Zapata Henríquez Barón Jaramillo Londoño " +
        "detrás después éxito Éxito ubicación urbanización último ésta éste allá allí aquí").Split(' ')
        .GroupBy(Quitar)
        .ToDictionary(g => g.Key, g => g.First());

    /// <summary>Corrige lo que pueda del texto. <paramref name="sinResolver"/> indica si quedó algún carácter dañado.</summary>
    public static string Corregir(string texto, out bool sinResolver)
    {
        sinResolver = false;
        if (string.IsNullOrEmpty(texto))
        {
            return texto;
        }

        texto = Regex.Replace(texto.Replace(' ', ' ').Replace("​", string.Empty), @"\s{2,}", " ").Trim();
        if (DobleCodificacion.IsMatch(texto))
        {
            var recuperado = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(texto));
            if (!recuperado.Contains(Reemplazo))
            {
                texto = recuperado;
            }
        }

        if (!texto.Contains(Reemplazo))
        {
            return texto;
        }

        var quedo = false;
        var partes = new List<string>();
        foreach (var parte in Regex.Split(texto, @"(?<=\s)|(?=\s)"))
        {
            partes.Add(CorregirPalabra(parte, ref quedo));
        }

        sinResolver = quedo;
        return Regex.Replace(string.Concat(partes), @"\s{2,}", " ").Trim();
    }

    private static string CorregirPalabra(string palabra, ref bool sinResolver)
    {
        if (!palabra.Contains(Reemplazo))
        {
            return palabra;
        }

        // Un carácter dañado pegado al final de una palabra o antes de una mayúscula es un espacio (antes era un espacio duro).
        var resultado = Regex.Replace(palabra, $@"(?<=\p{{Ll}}){Reemplazo}(?=\p{{Lu}})", " ");
        resultado = Regex.Replace(resultado, $"{Reemplazo}$", string.Empty);
        if (!resultado.Contains(Reemplazo))
        {
            return resultado;
        }

        // Primero se prueba con la lista: "Mar�a" es María, no "Marña".
        var conocida = ProbarVocales(resultado);
        if (conocida is not null)
        {
            return conocida;
        }

        // Entre una letra y una vocal, es una eñe ("Casta�o", "Mu�oz", "Pe�a").
        resultado = Regex.Replace(resultado, $@"(?<=\p{{L}}){Reemplazo}(?=[{Vocales}])", "ñ");
        if (!resultado.Contains(Reemplazo))
        {
            return resultado;
        }

        sinResolver = true;
        return resultado;
    }

    private static string? ProbarVocales(string palabra)
    {
        var candidatos = new List<string> { palabra };
        foreach (var _ in palabra.Where(c => c == Reemplazo))
        {
            candidatos = candidatos.SelectMany(c => VocalesConTilde.Select(v => ReemplazarPrimero(c, v))).ToList();
        }

        var coincidencias = candidatos.Where(c => Palabras.TryGetValue(Quitar(c), out var propia) && string.Equals(propia, c, StringComparison.OrdinalIgnoreCase)).Distinct().ToList();
        return coincidencias.Count == 1 ? AjustarMayusculas(palabra, coincidencias[0]) : null;
    }

    private static string ReemplazarPrimero(string texto, char vocal)
    {
        var posicion = texto.IndexOf(Reemplazo);
        return texto[..posicion] + vocal + texto[(posicion + 1)..];
    }

    /// <summary>Conserva las mayúsculas del texto original: si venía todo en mayúsculas, el resultado también.</summary>
    private static string AjustarMayusculas(string original, string corregida)
    {
        var letras = original.Where(char.IsLetter).ToList();
        return letras.Count > 1 && letras.All(char.IsUpper) ? corregida.ToUpperInvariant() : corregida;
    }

    private static string Quitar(string palabra) => TextoNormalizador.Normalizar(palabra);
}
