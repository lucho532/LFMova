using ClosedXML.Excel;
using LFMova.Application.DTOs.Importaciones;
using static LFMova.Infrastructure.Excel.CamposHoja;
using static LFMova.Infrastructure.Excel.CeldasHoja;

namespace LFMova.Infrastructure.Excel;

/// <summary>
/// Ubica la fila de encabezados de una hoja y lee los datos generales que van encima de ella
/// (transportador y fecha). No lee las filas de pasajeros.
/// </summary>
internal static class EncabezadosHoja
{
    internal static void LeerCabecera(IXLWorksheet hoja, int filaEncabezado, int ultimaColumna, HojaProgramacion resultado)
    {
        for (var fila = 1; fila < filaEncabezado; fila++)
        {
            for (var columna = 1; columna <= ultimaColumna; columna++)
            {
                var texto = Texto(hoja.Cell(fila, columna)).Trim().TrimEnd(':').ToUpperInvariant();
                if (texto == "TRANSPORTADOR" && resultado.Transportador.Length == 0)
                {
                    resultado.Transportador = SiguienteTexto(hoja, fila, columna, ultimaColumna);
                }
                else if (texto == "FECHA" && resultado.FechaTexto.Length == 0)
                {
                    resultado.FechaTexto = SiguienteTexto(hoja, fila, columna, ultimaColumna);
                }
            }
        }
    }

    internal static string SiguienteTexto(IXLWorksheet hoja, int fila, int desdeColumna, int ultimaColumna)
    {
        for (var columna = desdeColumna + 1; columna <= ultimaColumna; columna++)
        {
            var texto = Texto(hoja.Cell(fila, columna)).Trim();
            if (texto.Length > 0)
            {
                return texto;
            }
        }

        return string.Empty;
    }

    /// <summary>Busca, en las primeras filas, la que mejor reconoce como fila de encabezados.</summary>
    internal static Dictionary<string, int>? BuscarEncabezados(IXLWorksheet hoja, int ultimaFila, int ultimaColumna, out int filaEncabezado, out string diagnostico)
    {
        Dictionary<string, int>? mejor = null;
        var mejorFila = 0;

        for (var fila = 1; fila <= Math.Min(ultimaFila, 15); fila++)
        {
            var columnas = new Dictionary<string, int>();
            for (var columna = 1; columna <= ultimaColumna; columna++)
            {
                var campo = ReconocerCampo(Texto(hoja.Cell(fila, columna)));
                if (campo is not null && !columnas.ContainsKey(campo))
                {
                    columnas[campo] = columna;
                }
            }

            if (mejor is null || columnas.Count > mejor.Count)
            {
                mejor = columnas;
                mejorFila = fila;
            }

            if (Obligatorios.All(columnas.ContainsKey) && (columnas.ContainsKey(Entrada) || columnas.ContainsKey(Salida)))
            {
                filaEncabezado = fila;
                diagnostico = string.Empty;
                return columnas;
            }
        }

        filaEncabezado = 0;
        var leidos = mejor is null ? "ninguna" : string.Join(", ", mejor.Keys.Select(k => k.ToLowerInvariant()));
        var faltan = Obligatorios.Where(o => mejor is null || !mejor.ContainsKey(o)).Select(o => o.ToLowerInvariant()).ToList();
        if (mejor is null || (!mejor.ContainsKey(Entrada) && !mejor.ContainsKey(Salida)))
        {
            faltan.Add("hora de entrada o de salida");
        }

        diagnostico = $"Columnas reconocidas en la fila {mejorFila}: {leidos}. Faltan: {string.Join(", ", faltan)}.";
        return null;
    }
}
