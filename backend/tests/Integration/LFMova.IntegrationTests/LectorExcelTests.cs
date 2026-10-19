using ClosedXML.Excel;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Excel;

namespace LFMova.IntegrationTests;

/// <summary>
/// Pruebas del lector de Excel con encabezados y horas escritos de distintas
/// formas. No usan base de datos: construyen el archivo en memoria.
/// </summary>
public class LectorExcelTests
{
    private static MemoryStream Libro(Action<IXLWorksheet> llenar)
    {
        using var libro = new XLWorkbook();
        llenar(libro.AddWorksheet("Datos"));
        var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        memoria.Position = 0;
        return memoria;
    }

    private static void Fila(IXLWorksheet hoja, int fila, params object[] valores)
    {
        for (var i = 0; i < valores.Length; i++)
        {
            switch (valores[i])
            {
                case long l: hoja.Cell(fila, i + 1).Value = l; break;
                case int n: hoja.Cell(fila, i + 1).Value = n; break;
                case DateTime d: hoja.Cell(fila, i + 1).Value = d; break;
                case string s when s.Length > 0: hoja.Cell(fila, i + 1).Value = s; break;
            }
        }
    }

    [Fact]
    public void TablaPlana_ConEncabezadosDistintosYHorasEnTexto_SeAgrupaPorTipoSedeFechaYHora()
    {
        using var archivo = Libro(hoja =>
        {
            Fila(hoja, 1, "Cedula", "Nombre", "Apellidos", "Direccion", "Barrio", "Numero de celular", "Entrada", "Salida", "Fecha", "Sede");
            Fila(hoja, 2, 1000154005L, "Nicolas", "Marin Castaño", "Calle 116 # 63-44", "La Enea", 3128669351L, "", "4AM", "2026-09-27", "El Campin");
            Fila(hoja, 3, 1000154006L, "Ana", "Gomez", "Carrera 7 # 1-1", "Centro", 3001112233L, "4AM", "4:30 pm", new DateTime(2026, 9, 27), "El Campin");
            Fila(hoja, 4, 1000154007L, "Luis", "Rojas", "Calle 1 # 2-3", "Suba", 3009998877L, "", "4 a.m.", "27/09/2026", "el campin");
        });

        var hoja = new ExcelProgramacionLectorClosedXml().Leer(archivo);

        Assert.Empty(hoja.Errores);
        Assert.True(hoja.FechasPorFila);
        Assert.Equal("27/09/2026", hoja.FechaTexto);

        var salidas4 = Assert.Single(hoja.Grupos, g => g.Tipo == TipoServicio.SALIDA && g.Hora == new TimeOnly(4, 0));
        Assert.Equal(2, salidas4.Filas.Count);
        Assert.Equal(new DateOnly(2026, 9, 27), salidas4.Fecha);
        Assert.Single(hoja.Grupos, g => g.Tipo == TipoServicio.SALIDA && g.Hora == new TimeOnly(16, 30));
        var entrada = Assert.Single(hoja.Grupos, g => g.Tipo == TipoServicio.ENTRADA);
        Assert.Equal("Ana Gomez", entrada.Filas[0].NombreCompleto);
        Assert.Equal("3001112233", entrada.Filas[0].Celular);
        Assert.Equal(3, hoja.Grupos.SelectMany(g => g.Filas).Select(f => f.Cedula).Distinct().Count());
    }

    [Fact]
    public void EncabezadosConTildesMayusculasYUnErrorDeTipeo_SeReconocen()
    {
        using var archivo = Libro(hoja =>
        {
            Fila(hoja, 1, "Reporte diario");
            Fila(hoja, 2, "CÉDULA", "NOMBRES", "APELLIDO", "DIRECCIÓN", "BARIO", "TELÉFONO", "HORA ENTRADA", "HORA SALIDA", "DÍA", "PUNTO");
            Fila(hoja, 3, 55L, "Ana", "Perez", "Calle 1", "Centro", 300L, "06:00", "", "2026-09-27", "Norte");
        });

        var hoja = new ExcelProgramacionLectorClosedXml().Leer(archivo);

        Assert.Empty(hoja.Errores);
        var grupo = Assert.Single(hoja.Grupos);
        Assert.Equal(TipoServicio.ENTRADA, grupo.Tipo);
        Assert.Equal(new TimeOnly(6, 0), grupo.Hora);
    }

    [Fact]
    public void FilaConHoraInvalida_SeReportaConSuNumeroYLasDemasSeLeen()
    {
        using var archivo = Libro(hoja =>
        {
            Fila(hoja, 1, "Cedula", "Nombre", "Direccion", "Barrio", "Entrada", "Fecha", "Sede");
            Fila(hoja, 2, 1L, "Ana", "Calle 1", "Centro", "mañana", "2026-09-27", "Norte");
            Fila(hoja, 3, 2L, "Luis", "Calle 2", "Suba", "5AM", "2026-09-27", "Norte");
        });

        var hoja = new ExcelProgramacionLectorClosedXml().Leer(archivo);

        Assert.Contains(hoja.Errores, e => e.StartsWith("Fila 2:"));
        Assert.Single(hoja.Grupos.SelectMany(g => g.Filas));
    }

    [Fact]
    public void SinColumnaDeSedeNiSecciones_SeLeeComoTablaPlanaConSedeVacia()
    {
        using var archivo = Libro(hoja =>
        {
            Fila(hoja, 1, "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada");
            Fila(hoja, 2, 1L, "Ana", "Calle 1", "Centro", 300L, "06:00");
        });

        var hoja = new ExcelProgramacionLectorClosedXml().Leer(archivo);

        Assert.Empty(hoja.Errores);
        var grupo = Assert.Single(hoja.Grupos);
        Assert.Equal(string.Empty, grupo.NombreSede);
        Assert.Equal(TipoServicio.ENTRADA, grupo.Tipo);
    }

    [Fact]
    public void HojaConSecciones_FilasDeLaMismaHoraIntercaladasConOtraHora_SeUnenEnUnSoloGrupo()
    {
        using var archivo = Libro(hoja =>
        {
            Fila(hoja, 1, "Cedula", "Nombre", "Direccion", "Barrio", "Entrada");
            Fila(hoja, 2, "ENTRADA CENTRO");
            Fila(hoja, 3, 1L, "Ana", "Calle 1", "Centro", "06:00");
            Fila(hoja, 4, 2L, "Luis", "Calle 2", "Suba", "01:00");
            Fila(hoja, 5, 3L, "Marta", "Calle 3", "Norte", "06:00");
        });

        var hoja = new ExcelProgramacionLectorClosedXml().Leer(archivo);

        Assert.Empty(hoja.Errores);
        var grupoDeLasSeis = Assert.Single(hoja.Grupos, g => g.Hora == new TimeOnly(6, 0));
        Assert.Equal(2, grupoDeLasSeis.Filas.Count);
        Assert.Equal(new[] { "1", "3" }, grupoDeLasSeis.Filas.Select(f => f.Cedula));
        Assert.Single(hoja.Grupos, g => g.Hora == new TimeOnly(1, 0));
        Assert.Equal(2, hoja.Grupos.Count);
    }

    [Fact]
    public void SinLasColumnasNecesarias_ElMensajeIndicaQueFalta()
    {
        using var archivo = Libro(hoja =>
        {
            Fila(hoja, 1, "Cedula", "Nombre", "Direccion");
            Fila(hoja, 2, 1L, "Ana", "Calle 1");
        });

        var hoja = new ExcelProgramacionLectorClosedXml().Leer(archivo);

        var error = Assert.Single(hoja.Errores);
        Assert.Contains("barrio", error);
        Assert.Contains("hora de entrada o de salida", error);
    }
}
