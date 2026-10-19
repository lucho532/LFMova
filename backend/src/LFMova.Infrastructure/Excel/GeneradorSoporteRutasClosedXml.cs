using ClosedXML.Excel;
using LFMova.Application.DTOs.Soportes;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Domain.Enums;

namespace LFMova.Infrastructure.Excel;

/// <summary>
/// Implementa <see cref="IGeneradorSoporteRutas"/> con ClosedXML: una sola
/// hoja con los datos generales arriba y, debajo, un bloque por cada ruta
/// (título con sentido, sede, fecha y hora —y el conductor, si el soporte es
/// de toda la jornada—, y sus pasajeros en orden de recogida). No consulta datos ni decide qué rutas se incluyen.
/// </summary>
public class GeneradorSoporteRutasClosedXml : IGeneradorSoporteRutas
{
    private static readonly string[] Encabezados = { "#", "Cédula", "Nombre", "Teléfono", "Dirección", "Barrio" };

    /// <inheritdoc />
    public byte[] Generar(SoporteRutas soporte)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Rutas");

        hoja.Cell(1, 1).Value = $"Soporte de rutas · {soporte.NombreEmpresa}";
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(1, 1).Style.Font.FontSize = 14;
        hoja.Cell(2, 1).Value = soporte.NombreConductor is null ? "Todas las rutas de la jornada" : $"Conductor: {soporte.NombreConductor}";
        hoja.Cell(3, 1).Value = $"Jornada del {soporte.FechaOperativa:dd/MM/yyyy}";

        var fila = 5;
        foreach (var ruta in soporte.Rutas)
        {
            fila = EscribirRuta(hoja, fila, ruta) + 1;
        }

        hoja.Column(1).Width = 5;
        hoja.Column(2).Width = 16;
        hoja.Column(3).Width = 32;
        hoja.Column(4).Width = 16;
        hoja.Column(5).Width = 38;
        hoja.Column(6).Width = 24;

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    /// <summary>Escribe el bloque de una ruta a partir de la fila indicada y devuelve la primera fila libre después de él.</summary>
    private static int EscribirRuta(IXLWorksheet hoja, int fila, RutaSoporte ruta)
    {
        var sentido = ruta.Tipo == TipoServicio.ENTRADA ? "ENTRADA" : "SALIDA";
        var titulo = hoja.Range(fila, 1, fila, Encabezados.Length).Merge();
        var conductor = ruta.NombreConductor is null ? string.Empty : $" · Conductor: {ruta.NombreConductor}";
        titulo.Value = $"{sentido} {ruta.NombreSede.ToUpperInvariant()} · {ruta.Fecha:dd/MM/yyyy} · {FormatoOperacion.Hora(ruta.Hora)}{conductor}";
        titulo.Style.Font.Bold = true;
        titulo.Style.Font.FontColor = XLColor.White;
        titulo.Style.Fill.BackgroundColor = ruta.Tipo == TipoServicio.ENTRADA ? XLColor.FromHtml("#1D4ED8") : XLColor.FromHtml("#B45309");
        fila++;

        for (var columna = 0; columna < Encabezados.Length; columna++)
        {
            var celda = hoja.Cell(fila, columna + 1);
            celda.Value = Encabezados[columna];
            celda.Style.Font.Bold = true;
            celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
        }
        fila++;

        if (ruta.Pasajeros.Count == 0)
        {
            hoja.Cell(fila, 3).Value = "Sin pasajeros";
            fila++;
        }

        foreach (var pasajero in ruta.Pasajeros)
        {
            hoja.Cell(fila, 1).Value = pasajero.Orden;
            // Como texto: la cédula y el teléfono no son números con los que se opere y así no pierden ceros ni se abrevian.
            hoja.Cell(fila, 2).SetValue(pasajero.Cedula);
            hoja.Cell(fila, 3).Value = pasajero.NombreCompleto;
            hoja.Cell(fila, 4).SetValue(pasajero.Telefono);
            hoja.Cell(fila, 5).Value = pasajero.Direccion;
            hoja.Cell(fila, 6).Value = pasajero.Barrio;
            fila++;
        }

        return fila;
    }
}
