using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using LFMova.Application.DTOs.Importaciones;
using static LFMova.Infrastructure.Excel.CamposHoja;

namespace LFMova.Infrastructure.Excel;

/// <summary>
/// Interpreta el valor de una celda de la hoja: texto, cédula o celular, fecha y hora (acepta "4AM",
/// "4:30 pm", "16:00", "1600" y horas nativas de Excel), y arma la fila de un pasajero. No busca
/// encabezados ni agrupa filas.
/// </summary>
internal static class CeldasHoja
{
    private static readonly string[] FormatosFecha =
    {
        "yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yy", "d/M/yy", "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy HH:mm:ss"
    };

    private static readonly Regex HoraConMeridiano = new(@"^(\d{1,2})(?:\s*[:.hH]\s*(\d{1,2}))?\s*([AaPp])\.?\s*[Mm]?\.?$", RegexOptions.Compiled);
    private static readonly Regex HoraCuatroDigitos = new(@"^(\d{2})(\d{2})$", RegexOptions.Compiled);

    internal static FilaHoja CrearFila(IXLWorksheet hoja, int fila, string cedula, Dictionary<string, int> columnas)
    {
        string Leer(string campo) => columnas.TryGetValue(campo, out var columna) ? Texto(hoja.Cell(fila, columna)).Trim() : string.Empty;

        return new FilaHoja
        {
            NumeroFila = fila,
            Cedula = cedula,
            NombreCompleto = $"{Leer(Nombre)} {Leer(Apellidos)}".Trim().Replace("  ", " "),
            Direccion = Leer(Direccion),
            Barrio = Leer(Barrio),
            Celular = columnas.TryGetValue(Celular, out var columnaCelular) ? LimpiarNumero(hoja.Cell(fila, columnaCelular)) : string.Empty
        };
    }

    internal static string Texto(IXLCell celda) => celda.IsEmpty() ? string.Empty : celda.GetFormattedString();

    /// <summary>Lee una cédula o celular: números de Excel sin decimales y textos con puntos o espacios de separación.</summary>
    internal static string LimpiarNumero(IXLCell celda)
    {
        if (celda.IsEmpty())
        {
            return string.Empty;
        }

        if (celda.DataType == XLDataType.Number)
        {
            return Math.Round(celda.GetDouble()).ToString("0", CultureInfo.InvariantCulture);
        }

        var texto = celda.GetString().Trim();
        return Regex.IsMatch(texto, @"^[\d.\s,]+$") ? Regex.Replace(texto, @"\D", string.Empty) : texto;
    }

    internal static bool TryLeerFecha(IXLCell celda, out DateOnly fecha)
    {
        fecha = default;
        if (celda.DataType == XLDataType.DateTime)
        {
            fecha = DateOnly.FromDateTime(celda.GetDateTime());
            return true;
        }

        if (celda.DataType == XLDataType.Number)
        {
            fecha = DateOnly.FromDateTime(DateTime.FromOADate(celda.GetDouble()));
            return true;
        }

        return DateOnly.TryParseExact(celda.GetString().Trim(), FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
    }

    internal static bool TryLeerHora(IXLCell celda, out TimeOnly hora)
    {
        hora = default;
        if (celda.IsEmpty())
        {
            return false;
        }

        if (celda.DataType == XLDataType.TimeSpan)
        {
            hora = TimeOnly.FromTimeSpan(celda.GetTimeSpan());
            return true;
        }

        if (celda.DataType == XLDataType.DateTime)
        {
            hora = TimeOnly.FromDateTime(celda.GetDateTime());
            return true;
        }

        if (celda.DataType == XLDataType.Number)
        {
            var valor = celda.GetDouble();
            if (valor < 1)
            {
                hora = TimeOnly.FromTimeSpan(TimeSpan.FromDays(valor));
                return true;
            }

            if (valor < 24 && valor == Math.Floor(valor))
            {
                hora = new TimeOnly((int)valor, 0);
                return true;
            }

            var comoTexto = ((int)valor).ToString("0000", CultureInfo.InvariantCulture);
            return TryLeerHoraTexto(comoTexto, out hora);
        }

        return TryLeerHoraTexto(celda.GetString(), out hora);
    }

    /// <summary>Acepta "4AM", "4 a.m.", "4:30 pm", "16:00", "16:00:00", "1600" y "4".</summary>
    internal static bool TryLeerHoraTexto(string texto, out TimeOnly hora)
    {
        hora = default;
        texto = texto.Trim();

        var meridiano = HoraConMeridiano.Match(texto);
        if (meridiano.Success)
        {
            var horas = int.Parse(meridiano.Groups[1].Value, CultureInfo.InvariantCulture);
            var minutos = meridiano.Groups[2].Success ? int.Parse(meridiano.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
            if (horas is < 1 or > 12 || minutos > 59)
            {
                return false;
            }

            var esPm = char.ToUpperInvariant(meridiano.Groups[3].Value[0]) == 'P';
            hora = new TimeOnly(horas % 12 + (esPm ? 12 : 0), minutos);
            return true;
        }

        var cuatro = HoraCuatroDigitos.Match(texto);
        if (cuatro.Success)
        {
            var horas = int.Parse(cuatro.Groups[1].Value, CultureInfo.InvariantCulture);
            var minutos = int.Parse(cuatro.Groups[2].Value, CultureInfo.InvariantCulture);
            if (horas > 23 || minutos > 59)
            {
                return false;
            }

            hora = new TimeOnly(horas, minutos);
            return true;
        }

        if (int.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out var soloHora) && soloHora < 24)
        {
            hora = new TimeOnly(soloHora, 0);
            return true;
        }

        return TimeOnly.TryParse(texto, CultureInfo.InvariantCulture, out hora);
    }
}
