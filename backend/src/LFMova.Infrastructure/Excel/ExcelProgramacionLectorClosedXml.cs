using ClosedXML.Excel;
using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Interfaces;
using static LFMova.Infrastructure.Excel.CamposHoja;
using static LFMova.Infrastructure.Excel.EncabezadosHoja;
using static LFMova.Infrastructure.Excel.LectorHojaConSecciones;
using static LFMova.Infrastructure.Excel.LectorTablaPlana;

namespace LFMova.Infrastructure.Excel;

/// <summary>
/// Implementa <see cref="IExcelProgramacionLector"/> con ClosedXML y tolera
/// variaciones en los encabezados (sin tildes, mayúsculas, sinónimos como
/// "Numero de celular" o "Entrada"/"ENTRA", y errores de tipeo leves). Entiende
/// dos formatos, que detecta por sus columnas:
/// <list type="bullet">
/// <item><description><b>Tabla plana</b>: una fila por pasajero con columnas de Sede, Fecha y hora de
/// Entrada/Salida. Las filas se agrupan por tipo, sede, fecha y hora.</description></item>
/// <item><description><b>Hoja con secciones</b>: filas "ENTRADA &lt;SEDE&gt;" / "SALIDA &lt;SEDE&gt;" que
/// abren cada grupo, con "TRANSPORTADOR:" y "FECHA:" arriba.</description></item>
/// </list>
/// Las horas aceptan "4AM", "4:30 pm", "16:00", "1600" y horas nativas de Excel.
/// </summary>
public class ExcelProgramacionLectorClosedXml : IExcelProgramacionLector
{
    /// <inheritdoc />
    public HojaProgramacion Leer(Stream archivo)
    {
        using var libro = new XLWorkbook(archivo);
        var resultado = new HojaProgramacion();
        var diagnostico = string.Empty;

        foreach (var hoja in libro.Worksheets)
        {
            var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 0;
            var ultimaColumna = Math.Max(hoja.LastColumnUsed()?.ColumnNumber() ?? 0, 8);
            var columnas = BuscarEncabezados(hoja, ultimaFila, ultimaColumna, out var filaEncabezado, out var faltantes);

            if (columnas is null)
            {
                diagnostico = diagnostico.Length == 0 ? faltantes : diagnostico;
                continue;
            }

            LeerCabecera(hoja, filaEncabezado, ultimaColumna, resultado);
            // La sede ya no es obligatoria: si la hoja no trae columna de sede pero sí trae
            // secciones "ENTRADA/SALIDA <SEDE>", se leen esas; si no trae ninguna de las dos,
            // se lee como tabla plana sin sede (la elige el coordinador al importar).
            if (!columnas.ContainsKey(Sede) && TieneMarcasDeSeccion(hoja, filaEncabezado, ultimaFila, columnas))
            {
                LeerConSecciones(hoja, filaEncabezado, ultimaFila, columnas, resultado);
            }
            else
            {
                LeerTablaPlana(hoja, filaEncabezado, ultimaFila, columnas, resultado);
            }

            if (resultado.Grupos.Count == 0 && resultado.Errores.Count == 0)
            {
                resultado.Errores.Add("El archivo no contiene ningún pasajero.");
            }

            return resultado;
        }

        resultado.Errores.Add(
            "No se encontró la fila de encabezados. Se necesitan las columnas: cédula, nombre, dirección, barrio y hora de entrada o de salida "
            + $"(la sede y la fecha son opcionales: si faltan, se eligen al importar). {diagnostico}".TrimEnd());
        return resultado;
    }
}
