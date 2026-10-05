using System.Globalization;
using ClosedXML.Excel;
using LFMova.Application.DTOs.Facturacion;
using LFMova.Application.Interfaces;
using LFMova.Domain.Rules;

namespace LFMova.Infrastructure.Excel;

/// <summary>
/// Implementa <see cref="IGeneradorCierreMensual"/> con ClosedXML: una hoja
/// con los totales del mes de la empresa y, debajo, una fila por conductor
/// que finalizó rutas. Solo da formato: no consulta datos ni calcula cobros.
/// </summary>
public class GeneradorCierreMensualClosedXml : IGeneradorCierreMensual
{
    private static readonly string[] Encabezados =
        { "#", "Cédula", "Conductor", "Placas", "Rutas finalizadas", "Pasajeros transportados", "Primera ruta", "Última ruta", "Observación" };

    /// <inheritdoc />
    public byte[] Generar(DetalleFacturacionDto detalle)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Facturación");
        var mes = new DateOnly(detalle.Anio, detalle.Mes, 1).ToString("MMMM 'de' yyyy", CultureInfo.GetCultureInfo("es-CO"));

        hoja.Cell(1, 1).Value = $"LFMova · Uso de {detalle.Resumen.NombreEmpresa}";
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(1, 1).Style.Font.FontSize = 14;
        hoja.Cell(2, 1).Value = $"Mes: {mes}";
        hoja.Cell(3, 1).Value = detalle.Resumen.Cerrado && detalle.Resumen.FechaCierre is not null
            ? $"Mes cerrado el {ReglasHoraColombia.AhoraColombia(detalle.Resumen.FechaCierre.Value):yyyy-MM-dd HH:mm} (hora de Colombia)"
            : "Mes sin cerrar: los totales todavía pueden cambiar";

        Total(hoja, 5, "Conductores que finalizaron rutas", detalle.Resumen.ConductoresActivos);
        Total(hoja, 6, "Rutas finalizadas", detalle.Resumen.RutasFinalizadas);
        Total(hoja, 7, "Pasajeros transportados", detalle.Resumen.PasajerosTransportados);

        const int filaEncabezado = 9;
        for (var i = 0; i < Encabezados.Length; i++)
        {
            var celda = hoja.Cell(filaEncabezado, i + 1);
            celda.Value = Encabezados[i];
            celda.Style.Font.Bold = true;
            celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#E5EDFF");
        }

        var fila = filaEncabezado + 1;
        foreach (var conductor in detalle.Conductores)
        {
            hoja.Cell(fila, 1).Value = fila - filaEncabezado;
            hoja.Cell(fila, 2).SetValue(conductor.Cedula);
            hoja.Cell(fila, 3).Value = conductor.NombreConductor;
            hoja.Cell(fila, 4).Value = string.Join(", ", conductor.Placas);
            hoja.Cell(fila, 5).Value = conductor.RutasFinalizadas;
            hoja.Cell(fila, 6).Value = conductor.PasajerosTransportados;
            hoja.Cell(fila, 7).Value = conductor.PrimeraRuta.ToString("yyyy-MM-dd");
            hoja.Cell(fila, 8).Value = conductor.UltimaRuta.ToString("yyyy-MM-dd");
            hoja.Cell(fila, 9).Value = conductor.Eliminado ? "Cuenta eliminada después de sus rutas" : string.Empty;
            fila++;
        }

        hoja.Columns().AdjustToContents();
        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static void Total(IXLWorksheet hoja, int fila, string etiqueta, int valor)
    {
        hoja.Cell(fila, 1).Value = etiqueta;
        hoja.Cell(fila, 3).Value = valor;
        hoja.Cell(fila, 3).Style.Font.Bold = true;
    }
}
