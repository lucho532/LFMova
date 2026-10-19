using ClosedXML.Excel;
using LFMova.Application.DTOs.Soportes;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Infrastructure.Excel;

/// <summary>
/// Implementa <see cref="IGeneradorInformeAsistenciaPeriodo"/> con ClosedXML: una hoja con el período y el
/// filtro arriba y una tabla con un renglón por pasajero (ruta, conductor, datos del pasajero, estado,
/// hora y espera). No consulta datos ni decide qué pasajeros incluye.
/// </summary>
public class GeneradorInformeAsistenciaPeriodoClosedXml : IGeneradorInformeAsistenciaPeriodo
{
    private static readonly string[] Encabezados =
    {
        "Fecha", "Hora de la ruta", "Tipo", "Sede", "Conductor", "Placa",
        "Cédula", "Pasajero", "Teléfono", "Dirección", "Barrio", "Estado", "Hora de registro", "Espera (mm:ss)"
    };

    private static readonly int[] Anchos = { 12, 14, 10, 20, 26, 10, 14, 30, 14, 36, 20, 14, 16, 14 };

    /// <inheritdoc />
    public byte[] Generar(InformeAsistenciaPeriodo informe)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Asistencia");

        hoja.Cell(1, 1).Value = $"Informe de asistencia · {informe.NombreEmpresa}";
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(1, 1).Style.Font.FontSize = 14;
        hoja.Cell(2, 1).Value = informe.Desde == informe.Hasta
            ? $"Jornada del {informe.Desde:dd/MM/yyyy}"
            : $"Del {informe.Desde:dd/MM/yyyy} al {informe.Hasta:dd/MM/yyyy}";
        hoja.Cell(3, 1).Value = $"{TextoFiltro(informe.Filtro)} · {informe.Filas.Count} pasajeros";

        const int filaEncabezados = 5;
        for (var columna = 0; columna < Encabezados.Length; columna++)
        {
            var celda = hoja.Cell(filaEncabezados, columna + 1);
            celda.Value = Encabezados[columna];
            celda.Style.Font.Bold = true;
            celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
            hoja.Column(columna + 1).Width = Anchos[columna];
        }

        var fila = filaEncabezados + 1;
        foreach (var pasajero in informe.Filas)
        {
            EscribirFila(hoja, fila++, pasajero);
        }

        if (informe.Filas.Count == 0)
        {
            hoja.Cell(fila, 1).Value = "Ningún pasajero cumple este filtro en el período.";
        }

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static void EscribirFila(IXLWorksheet hoja, int fila, FilaInformeAsistenciaPeriodo pasajero)
    {
        hoja.Cell(fila, 1).SetValue(pasajero.Fecha.ToString("dd/MM/yyyy"));
        hoja.Cell(fila, 2).SetValue(pasajero.HoraRuta.ToString("HH:mm"));
        hoja.Cell(fila, 3).Value = pasajero.Tipo == TipoServicio.ENTRADA ? "Entrada" : "Salida";
        hoja.Cell(fila, 4).Value = pasajero.Sede;
        hoja.Cell(fila, 5).Value = pasajero.Conductor ?? "Sin conductor";
        hoja.Cell(fila, 6).Value = pasajero.Placa ?? string.Empty;
        // Como texto: la cédula y el teléfono no son números con los que se opere y así no pierden ceros.
        hoja.Cell(fila, 7).SetValue(pasajero.Cedula);
        hoja.Cell(fila, 8).Value = pasajero.NombreCompleto;
        hoja.Cell(fila, 9).SetValue(pasajero.Telefono);
        hoja.Cell(fila, 10).Value = pasajero.Direccion;
        hoja.Cell(fila, 11).Value = pasajero.Barrio;

        var celdaEstado = hoja.Cell(fila, 12);
        celdaEstado.Value = TextoEstado(pasajero.Estado);
        celdaEstado.Style.Font.FontColor = pasajero.Estado switch
        {
            EstadoServicioPasajero.RECOGIDO => XLColor.FromHtml("#067647"),
            EstadoServicioPasajero.NO_RECOGIDO => XLColor.FromHtml("#B42318"),
            _ => XLColor.Black
        };

        if (pasajero.HoraRegistro is not null)
        {
            hoja.Cell(fila, 13).SetValue(pasajero.HoraRegistro.Value.ToString("HH:mm"));
        }

        if (pasajero.EsperaSegundos is not null)
        {
            var segundos = pasajero.EsperaSegundos.Value;
            var celdaEspera = hoja.Cell(fila, 14);
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
