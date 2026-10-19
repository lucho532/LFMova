using ClosedXML.Excel;
using LFMova.Application.DTOs.Soportes;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Excel;

namespace LFMova.IntegrationTests;

/// <summary>
/// Pruebas del Excel del informe de asistencia por período: se genera y se vuelve a leer para comprobar el
/// encabezado, las columnas y los valores de cada pasajero. No necesitan base de datos.
/// </summary>
public class GeneradorInformeAsistenciaPeriodoTests
{
    private static InformeAsistenciaPeriodo Informe(DateOnly desde, DateOnly hasta, params FilaInformeAsistenciaPeriodo[] filas) => new()
    {
        NombreEmpresa = "Panamericana", Desde = desde, Hasta = hasta, Filtro = FiltroAsistenciaRuta.NO_ASISTIERON, Filas = filas.ToList()
    };

    private static IXLWorksheet Leer(InformeAsistenciaPeriodo informe)
        => new XLWorkbook(new MemoryStream(new GeneradorInformeAsistenciaPeriodoClosedXml().Generar(informe))).Worksheet("Asistencia");

    [Fact]
    public void Generar_UnSoloDia_LoLlamaJornada_YEscribeLosEncabezados()
    {
        var hoja = Leer(Informe(new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 15)));

        Assert.Equal("Informe de asistencia · Panamericana", hoja.Cell(1, 1).GetString());
        Assert.Equal("Jornada del 15/10/2026", hoja.Cell(2, 1).GetString());
        Assert.Contains("Solo los que no asistieron", hoja.Cell(3, 1).GetString());
        Assert.Equal(
            new[] { "Fecha", "Hora de la ruta", "Tipo", "Sede", "Conductor", "Placa", "Cédula", "Pasajero", "Teléfono", "Dirección", "Barrio", "Estado", "Hora de registro", "Espera (mm:ss)" },
            Enumerable.Range(1, 14).Select(c => hoja.Cell(5, c).GetString()));
        Assert.Equal("Ningún pasajero cumple este filtro en el período.", hoja.Cell(6, 1).GetString());
    }

    [Fact]
    public void Generar_VariosDias_IndicaElRango()
    {
        var hoja = Leer(Informe(new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 17)));

        Assert.Equal("Del 15/10/2026 al 17/10/2026", hoja.Cell(2, 1).GetString());
    }

    [Fact]
    public void Generar_EscribeCadaPasajeroConSuRutaConductorYHorarios()
    {
        var hoja = Leer(Informe(new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 15),
            new FilaInformeAsistenciaPeriodo
            {
                Fecha = new DateOnly(2026, 10, 15), HoraRuta = new TimeOnly(6, 0), Tipo = TipoServicio.ENTRADA, Sede = "La Patria",
                Conductor = "Andre Otalvaro", Placa = "STT123", Cedula = "0012345", NombreCompleto = "Ana", Telefono = "3001112222",
                Direccion = "Calle 55C # 11A-21", Barrio = "Centro", Estado = EstadoServicioPasajero.NO_RECOGIDO,
                HoraRegistro = new DateTime(2026, 10, 15, 6, 2, 44), EsperaSegundos = 164
            },
            new FilaInformeAsistenciaPeriodo
            {
                Fecha = new DateOnly(2026, 10, 15), HoraRuta = new TimeOnly(14, 30), Tipo = TipoServicio.SALIDA, Sede = "Panamericana",
                Conductor = null, Placa = null, Cedula = "77", NombreCompleto = "Eva", Estado = EstadoServicioPasajero.NO_ASISTIRA
            }));

        Assert.Equal("15/10/2026", hoja.Cell(6, 1).GetString());
        Assert.Equal("06:00", hoja.Cell(6, 2).GetString());
        Assert.Equal("Entrada", hoja.Cell(6, 3).GetString());
        Assert.Equal("Andre Otalvaro", hoja.Cell(6, 5).GetString());
        Assert.Equal("STT123", hoja.Cell(6, 6).GetString());
        Assert.Equal("0012345", hoja.Cell(6, 7).GetString());
        Assert.Equal("3001112222", hoja.Cell(6, 9).GetString());
        Assert.Equal("Calle 55C # 11A-21", hoja.Cell(6, 10).GetString());
        Assert.Equal("No recogido", hoja.Cell(6, 12).GetString());
        Assert.Equal("06:02", hoja.Cell(6, 13).GetString());
        Assert.Equal("02:44", hoja.Cell(6, 14).GetString());
        // Una ruta sin conductor lo dice, y quien no llegó a procesarse no lleva hora ni espera.
        Assert.Equal("Salida", hoja.Cell(7, 3).GetString());
        Assert.Equal("Sin conductor", hoja.Cell(7, 5).GetString());
        Assert.Equal("No asistirá", hoja.Cell(7, 12).GetString());
        Assert.True(hoja.Cell(7, 13).IsEmpty());
        Assert.True(hoja.Cell(7, 14).IsEmpty());
    }
}
