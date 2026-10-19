using ClosedXML.Excel;
using LFMova.Application.DTOs.Soportes;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Domain.Enums;

namespace LFMova.Infrastructure.Excel;

/// <summary>
/// Implementa <see cref="IGeneradorInformeAsistencia"/> con ClosedXML: una hoja con los datos de la ruta
/// arriba y, debajo, la tabla de pasajeros (número, cédula, nombre, estado, hora de recogida y espera).
/// No consulta datos ni decide qué pasajeros incluye.
/// </summary>
public class GeneradorInformeAsistenciaClosedXml : IGeneradorInformeAsistencia
{
    private static readonly string[] Encabezados = { "#", "Cédula", "Nombre", "Estado", "Hora de recogida", "Espera (mm:ss)" };

    /// <inheritdoc />
    public byte[] Generar(InformeAsistenciaRuta informe)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Asistencia");

        var sentido = informe.Tipo == TipoServicio.ENTRADA ? "Entrada" : "Salida";
        hoja.Cell(1, 1).Value = $"Informe de asistencia · {informe.NombreEmpresa}";
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(1, 1).Style.Font.FontSize = 14;
        hoja.Cell(2, 1).Value = $"Ruta de {sentido.ToLowerInvariant()} · {informe.NombreSede} · {informe.Fecha:dd/MM/yyyy} · {FormatoOperacion.Hora(informe.Hora)}";
        hoja.Cell(3, 1).Value = informe.NombreConductor is null ? "Sin conductor asignado" : $"Conductor: {informe.NombreConductor}";
        hoja.Cell(4, 1).Value = $"{TextoFiltro(informe.Filtro)} · {informe.Filas.Count} de {informe.TotalPasajerosRuta} pasajeros de la ruta";

        const int filaEncabezados = 6;
        for (var columna = 0; columna < Encabezados.Length; columna++)
        {
            var celda = hoja.Cell(filaEncabezados, columna + 1);
            celda.Value = Encabezados[columna];
            celda.Style.Font.Bold = true;
            celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
        }

        var fila = filaEncabezados + 1;
        foreach (var pasajero in informe.Filas)
        {
            EscribirFila(hoja, fila++, pasajero);
        }

        if (informe.Filas.Count == 0)
        {
            hoja.Cell(fila, 3).Value = "Ningún pasajero cumple este filtro.";
        }

        hoja.Column(1).Width = 5;
        hoja.Column(2).Width = 16;
        hoja.Column(3).Width = 34;
        hoja.Column(4).Width = 16;
        hoja.Column(5).Width = 18;
        hoja.Column(6).Width = 16;

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static void EscribirFila(IXLWorksheet hoja, int fila, FilaInformeAsistencia pasajero)
    {
        if (pasajero.Numero is not null)
        {
            hoja.Cell(fila, 1).Value = pasajero.Numero.Value;
        }

        // Como texto: la cédula no es un número con el que se opere y así no pierde ceros.
        hoja.Cell(fila, 2).SetValue(pasajero.Cedula);
        hoja.Cell(fila, 3).Value = pasajero.NombreCompleto;
        var celdaEstado = hoja.Cell(fila, 4);
        celdaEstado.Value = TextoEstado(pasajero.Estado);
        celdaEstado.Style.Font.FontColor = pasajero.Estado switch
        {
            EstadoServicioPasajero.RECOGIDO => XLColor.FromHtml("#067647"),
            EstadoServicioPasajero.NO_RECOGIDO => XLColor.FromHtml("#B42318"),
            _ => XLColor.Black
        };

        if (pasajero.HoraRegistro is not null)
        {
            hoja.Cell(fila, 5).SetValue(pasajero.HoraRegistro.Value.ToString("HH:mm"));
        }

        if (pasajero.EsperaSegundos is not null)
        {
            var segundos = pasajero.EsperaSegundos.Value;
            var celdaEspera = hoja.Cell(fila, 6);
            celdaEspera.SetValue($"{segundos / 60:00}:{segundos % 60:00}");
            // Mismo criterio que en pantalla: hasta 2 minutos es a tiempo.
            celdaEspera.Style.Font.FontColor = segundos > 120 ? XLColor.FromHtml("#B42318") : XLColor.FromHtml("#067647");
        }
    }

    private static string TextoFiltro(FiltroAsistenciaRuta filtro) => filtro switch
    {
        FiltroAsistenciaRuta.ASISTIERON => "Solo los que asistieron",
        FiltroAsistenciaRuta.NO_ASISTIERON => "Solo los que no asistieron",
        _ => "Todos los pasajeros"
    };

    private static string TextoEstado(EstadoServicioPasajero estado) => estado switch
    {
        EstadoServicioPasajero.PROGRAMADO => "Programado",
        EstadoServicioPasajero.CONFIRMADO => "Confirmado",
        EstadoServicioPasajero.NO_ASISTIRA => "No asistirá",
        EstadoServicioPasajero.CONDUCTOR_LLEGO => "Conductor llegó",
        EstadoServicioPasajero.RECOGIDO => "Recogido",
        EstadoServicioPasajero.NO_RECOGIDO => "No recogido",
        _ => "Cancelado"
    };
}
