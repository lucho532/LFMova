using ClosedXML.Excel;
using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Infrastructure.Excel;

/// <summary>
/// Implementa <see cref="IExcelRutasArmadasLector"/> con ClosedXML. Reutiliza el reconocimiento tolerante
/// de encabezados del lector de programación (sin tildes, sinónimos, errores leves) y le suma las
/// columnas de número de ruta y placa. Lee la primera hoja que traiga las columnas necesarias. Las
/// rutas se respetan tal como vienen: no se reparten, reordenan ni dividen.
/// </summary>
public class ExcelRutasArmadasLectorClosedXml : IExcelRutasArmadasLector
{
    /// <inheritdoc />
    public HojaRutasArmadas Leer(Stream archivo, ModoSeparacionRutas modo, string? textoInicioRuta)
    {
        using var libro = new XLWorkbook(archivo);
        HojaRutasArmadas? primera = null;
        foreach (var hoja in libro.Worksheets)
        {
            var resultado = LectorRutasArmadas.Leer(hoja, modo, textoInicioRuta);
            if (resultado.Errores.Count == 0)
            {
                return resultado;
            }

            primera ??= resultado;
        }

        return primera ?? new HojaRutasArmadas { Errores = { "El archivo no tiene hojas." } };
    }
}
