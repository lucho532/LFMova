using ClosedXML.Excel;
using LFMova.Application.DTOs.Soportes;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Excel;

namespace LFMova.IntegrationTests;

/// <summary>
/// Pruebas del Excel del informe de asistencia: se genera y se vuelve a leer para comprobar el
/// encabezado de la ruta, las columnas y los valores de cada pasajero. No necesitan base de datos.
/// </summary>
public class GeneradorInformeAsistenciaTests
{
    private static InformeAsistenciaRuta Informe(FiltroAsistenciaRuta filtro, params FilaInformeAsistencia[] filas) => new()
    {
        NombreEmpresa = "Panamericana",
        Tipo = TipoServicio.ENTRADA,
        NombreSede = "La Patria",
        Fecha = new DateOnly(2026, 10, 15),
        Hora = new TimeOnly(6, 0),
        NombreConductor = "Andre Otalvaro",
        Filtro = filtro,
        TotalPasajerosRuta = 5,
        Filas = filas.ToList()
    };

    private static IXLWorksheet Leer(byte[] contenido)
    {
        var libro = new XLWorkbook(new MemoryStream(contenido));
        return libro.Worksheet("Asistencia");
    }

    [Fact]
    public void Generar_EscribeLosDatosDeLaRutaYLosEncabezados()
    {
        var hoja = Leer(new GeneradorInformeAsistenciaClosedXml().Generar(Informe(FiltroAsistenciaRuta.NO_ASISTIERON)));

        Assert.Equal("Informe de asistencia · Panamericana", hoja.Cell(1, 1).GetString());
        Assert.Contains("La Patria", hoja.Cell(2, 1).GetString());
        Assert.Contains("15/10/2026", hoja.Cell(2, 1).GetString());
        Assert.Equal("Conductor: Andre Otalvaro", hoja.Cell(3, 1).GetString());
        Assert.Contains("Solo los que no asistieron", hoja.Cell(4, 1).GetString());
        Assert.Equal(new[] { "#", "Cédula", "Nombre", "Estado", "Hora de recogida", "Espera (mm:ss)" }, Enumerable.Range(1, 6).Select(c => hoja.Cell(6, c).GetString()));
    }

    [Fact]
    public void Generar_EscribeCadaPasajeroConSuEstadoHoraYEspera()
    {
        var hoja = Leer(new GeneradorInformeAsistenciaClosedXml().Generar(Informe(
            FiltroAsistenciaRuta.TODOS,
            new FilaInformeAsistencia { Numero = 1, Cedula = "0012345", NombreCompleto = "Ana", Estado = EstadoServicioPasajero.RECOGIDO, HoraRegistro = new DateTime(2026, 10, 15, 6, 2, 44), EsperaSegundos = 52 },
            new FilaInformeAsistencia { Numero = 2, Cedula = "99", NombreCompleto = "Luis", Estado = EstadoServicioPasajero.NO_RECOGIDO, HoraRegistro = new DateTime(2026, 10, 15, 6, 4, 0), EsperaSegundos = 164 },
            new FilaInformeAsistencia { Numero = null, Cedula = "77", NombreCompleto = "Eva", Estado = EstadoServicioPasajero.NO_ASISTIRA })));

        Assert.Equal(1, hoja.Cell(7, 1).GetDouble());
        Assert.Equal("0012345", hoja.Cell(7, 2).GetString());
        Assert.Equal("Recogido", hoja.Cell(7, 4).GetString());
        Assert.Equal("06:02", hoja.Cell(7, 5).GetString());
        Assert.Equal("00:52", hoja.Cell(7, 6).GetString());
        Assert.Equal("No recogido", hoja.Cell(8, 4).GetString());
        Assert.Equal("02:44", hoja.Cell(8, 6).GetString());
        // Quien no llegó a procesarse no lleva número, hora ni espera.
        Assert.True(hoja.Cell(9, 1).IsEmpty());
        Assert.Equal("No asistirá", hoja.Cell(9, 4).GetString());
        Assert.True(hoja.Cell(9, 5).IsEmpty());
        Assert.True(hoja.Cell(9, 6).IsEmpty());
    }

    [Fact]
    public void Generar_AvisaCuandoNingunPasajeroCumpleElFiltro()
    {
        var hoja = Leer(new GeneradorInformeAsistenciaClosedXml().Generar(Informe(FiltroAsistenciaRuta.NO_ASISTIERON)));

        Assert.Equal("Ningún pasajero cumple este filtro.", hoja.Cell(7, 3).GetString());
    }
}
