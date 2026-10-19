using System.Globalization;
using System.Text.RegularExpressions;
using LFMova.Domain.Enums;

namespace LFMova.Application.Utils;

/// <summary>
/// Extrae de la línea de texto que abre una ruta los datos que pueda traer: la hora ("5:00", "5 am",
/// "16:30"), la placa ("STQ789") y si es ENTRADA o SALIDA. Lo que la línea no traiga se toma de las
/// columnas de cada pasajero. No lee el Excel ni valida los datos contra la empresa.
/// </summary>
public static class InterpretadorLineaInicioRuta
{
    private static readonly Regex Hora = new(
        @"(?<![\d:.])(?<h>\d{1,2})(?:\s*[:.]\s*(?<m>\d{2}))?\s*(?<ap>[ap])\.?\s*m\b\.?|(?<![\d:.])(?<h>\d{1,2})\s*[:.]\s*(?<m>\d{2})(?![\d])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Placa = new(@"\b[A-Za-z]{3}[- ]?\d{2}[A-Za-z0-9]\b", RegexOptions.Compiled);

    /// <summary>Datos que una línea de inicio puede traer; cada uno es <c>null</c> si la línea no lo menciona.</summary>
    public sealed record DatosLinea(TimeOnly? Hora, string? Placa, TipoServicio? Tipo);

    /// <summary>Interpreta la línea de inicio de una ruta.</summary>
    public static DatosLinea Interpretar(string linea)
    {
        var normalizada = TextoNormalizador.Normalizar(linea);
        TipoServicio? tipo = null;
        if (Regex.IsMatch(normalizada, @"\bENTRADA\b|\bENTRA\b"))
        {
            tipo = TipoServicio.ENTRADA;
        }
        else if (Regex.IsMatch(normalizada, @"\bSALIDA\b|\bSALE\b"))
        {
            tipo = TipoServicio.SALIDA;
        }

        var placa = Placa.Match(linea);
        return new DatosLinea(LeerHora(linea), placa.Success ? Regex.Replace(placa.Value, @"[\s-]", string.Empty).ToUpperInvariant() : null, tipo);
    }

    private static TimeOnly? LeerHora(string linea)
    {
        var coincidencia = Hora.Match(linea);
        if (!coincidencia.Success)
        {
            return null;
        }

        var hora = int.Parse(coincidencia.Groups["h"].Value, CultureInfo.InvariantCulture);
        var minuto = coincidencia.Groups["m"].Success ? int.Parse(coincidencia.Groups["m"].Value, CultureInfo.InvariantCulture) : 0;
        if (coincidencia.Groups["ap"].Success)
        {
            var esPm = char.ToLowerInvariant(coincidencia.Groups["ap"].Value[0]) == 'p';
            hora = hora % 12 + (esPm ? 12 : 0);
        }

        return hora is < 24 && minuto is < 60 ? new TimeOnly(hora, minuto) : null;
    }
}
