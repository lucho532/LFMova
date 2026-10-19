using ClosedXML.Excel;
using LFMova.Domain.Enums;
using static LFMova.Infrastructure.Excel.CamposHoja;
using static LFMova.Infrastructure.Excel.CeldasHoja;

namespace LFMova.Infrastructure.Excel;

/// <summary>
/// Ubica las columnas de un Excel con rutas armadas y lee los datos de ruta de una fila (hora, tipo,
/// fecha, sede, placa y número de ruta). Los encabezados pueden estar repartidos en varias filas
/// seguidas. No agrupa las filas en rutas ni valida nada contra la empresa.
/// </summary>
internal static class ColumnasRutasArmadas
{
    private const int FilasDeEncabezado = 15;
    private const int LargoMaximoEncabezado = 25;
    private const double CedulaMinima = 100_000;

    /// <summary>
    /// Reconoce las columnas mirando las primeras filas, hasta la primera que ya trae una cédula numérica.
    /// Solo cuentan los textos cortos (un encabezado nunca es una dirección) y gana la primera aparición.
    /// </summary>
    internal static Dictionary<string, int> Buscar(IXLWorksheet hoja, int ultimaFila, int ultimaColumna)
    {
        var columnas = new Dictionary<string, int>();
        for (var fila = 1; fila <= Math.Min(ultimaFila, FilasDeEncabezado); fila++)
        {
            if (Enumerable.Range(1, ultimaColumna).Any(c => hoja.Cell(fila, c).DataType == XLDataType.Number && hoja.Cell(fila, c).GetDouble() >= CedulaMinima))
            {
                break;
            }

            for (var columna = 1; columna <= ultimaColumna; columna++)
            {
                var texto = Texto(hoja.Cell(fila, columna));
                var campo = texto.Length is > 0 and <= LargoMaximoEncabezado ? ReconocerCampo(texto) : null;
                if (campo is not null)
                {
                    RegistrarColumna(columnas, campo, columna);
                }
            }
        }

        return columnas;
    }

    /// <summary>Describe qué campos faltan para poder leer el archivo en el modo pedido, o vacío si están todos.</summary>
    internal static string Faltantes(Dictionary<string, int> columnas, ModoSeparacionRutas modo)
    {
        var faltan = Obligatorios.Where(o => !columnas.ContainsKey(o)).Select(o => o.ToLowerInvariant()).ToList();
        // Con líneas de inicio la hora puede venir en la línea; sin ella, cada ruta se rechaza más adelante.
        if (modo == ModoSeparacionRutas.COLUMNA_NUMERO_RUTA && !columnas.ContainsKey(Entrada) && !columnas.ContainsKey(Salida))
        {
            faltan.Add("hora de entrada o de salida");
        }

        if (modo == ModoSeparacionRutas.COLUMNA_NUMERO_RUTA && !columnas.ContainsKey(NumeroRuta))
        {
            faltan.Add("número de ruta");
        }

        return string.Join(", ", faltan);
    }

    /// <summary>Indica si la fila es de un pasajero: cédula numérica y nombre escrito como texto (así se descartan encabezados repetidos y filas sueltas).</summary>
    internal static bool EsPasajero(IXLWorksheet hoja, int fila, Dictionary<string, int> columnas, out string cedula)
    {
        cedula = LimpiarNumero(hoja.Cell(fila, columnas[Cedula]));
        var nombre = hoja.Cell(fila, columnas[Nombre]);
        return cedula.Length > 0 && cedula.All(char.IsDigit) && !nombre.IsEmpty() && nombre.DataType == XLDataType.Text && nombre.GetString().Trim().Length > 0;
    }

    /// <summary>Lee la hora de la fila: la columna de entrada o la de salida. Devuelve <c>false</c> con <paramref name="error"/> si hay dos horas o una inválida; ambas vacías no es error.</summary>
    internal static bool TryLeerHoraYTipo(IXLWorksheet hoja, int fila, Dictionary<string, int> columnas, out (TipoServicio Tipo, TimeOnly Hora)? horario, out string? error)
    {
        horario = null;
        error = null;
        foreach (var (campo, tipo) in new[] { (Entrada, TipoServicio.ENTRADA), (Salida, TipoServicio.SALIDA) })
        {
            if (!columnas.TryGetValue(campo, out var columna) || hoja.Cell(fila, columna).IsEmpty())
            {
                continue;
            }

            if (!TryLeerHora(hoja.Cell(fila, columna), out var hora))
            {
                error = $"Fila {fila}: la hora de {campo.ToLowerInvariant()} \"{Texto(hoja.Cell(fila, columna)).Trim()}\" no es válida.";
                return false;
            }

            if (horario is not null)
            {
                error = $"Fila {fila}: trae hora de entrada y de salida a la vez.";
                return false;
            }

            horario = (tipo, hora);
        }

        return true;
    }

    internal static string LeerTexto(IXLWorksheet hoja, int fila, Dictionary<string, int> columnas, string campo)
        => columnas.TryGetValue(campo, out var columna) ? Texto(hoja.Cell(fila, columna)).Trim() : string.Empty;
}
