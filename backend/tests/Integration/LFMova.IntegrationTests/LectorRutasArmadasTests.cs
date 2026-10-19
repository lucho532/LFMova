using ClosedXML.Excel;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Excel;

namespace LFMova.IntegrationTests;

/// <summary>
/// Pruebas del lector de Excel con rutas armadas, con una hoja parecida a la que genera la herramienta
/// externa del cliente: encabezados repartidos en dos filas, línea con el nombre del conductor, encabezados
/// repetidos y una ruta partida en dos bloques. No usan base de datos: construyen el archivo en memoria.
/// </summary>
public class LectorRutasArmadasTests
{
    private static readonly DateTime Dia = new(2026, 12, 27);

    private static MemoryStream Libro(Action<IXLWorksheet> llenar)
    {
        using var libro = new XLWorkbook();
        llenar(libro.AddWorksheet("Hoja1"));
        var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        memoria.Position = 0;
        return memoria;
    }

    private static void Poner(IXLWorksheet hoja, int fila, params (int Columna, object Valor)[] celdas)
    {
        foreach (var (columna, valor) in celdas)
        {
            switch (valor)
            {
                case long l: hoja.Cell(fila, columna).Value = l; break;
                case int n: hoja.Cell(fila, columna).Value = n; break;
                case DateTime d: hoja.Cell(fila, columna).Value = d; break;
                case TimeSpan t: hoja.Cell(fila, columna).Value = t; break;
                case string s: hoja.Cell(fila, columna).Value = s; break;
            }
        }
    }

    private static void Encabezado(IXLWorksheet hoja, int fila)
    {
        Poner(hoja, fila, (1, "ID_EMPLEADO"), (2, "NOMBRE"), (3, "COD_SERVICIO"), (4, "DIRECCION"), (5, "BARRIO"), (6, "TELEFONO"), (7, "CELULAR"));
        Poner(hoja, fila + 1, (1, 3), (2, Dia), (8, "Hora Entrada"), (9, "Hora Salida"), (10, "FECHA "), (11, "No RUTA"), (12, "PLACA "));
    }

    private static void Pasajero(IXLWorksheet hoja, int fila, long cedula, string nombre, int ruta, string placa, int hora = 5, string barrio = "Centro")
        => Poner(hoja, fila, (1, cedula), (2, nombre), (3, 390001), (4, "Calle 1 No. 2 - 3"), (5, barrio), (7, 3100000000L), (8, new TimeSpan(hora, 0, 0)), (10, Dia), (11, ruta), (12, placa));

    [Fact]
    public void PorNumeroDeRuta_CadaBloqueEsSuPropiaRutaAunqueRepitaElNumero()
    {
        using var archivo = Libro(hoja =>
        {
            Poner(hoja, 2, (2, "JUAN CARLOS"));
            Encabezado(hoja, 3);
            Pasajero(hoja, 5, 1057787629, "Dahiana Salazar", 1, "STQ789");
            Pasajero(hoja, 6, 1059812320, "Yuliana Marin", 1, "STQ789");
            Poner(hoja, 8, (2, "LUIS"));
            Encabezado(hoja, 9);
            Pasajero(hoja, 11, 52192462, "Omaira Hernandez", 7, "USE532", hora: 6);
            Poner(hoja, 13, (2, "LEONARDO"));
            Encabezado(hoja, 14);
            Pasajero(hoja, 16, 1053764960, "Luz Matinez", 12, "VEW827", hora: 6);
            Poner(hoja, 18, (2, "LUIS"));
            Pasajero(hoja, 19, 30237530, "Maria Hernandez", 7, "USE532", hora: 6);
        });

        var resultado = new ExcelRutasArmadasLectorClosedXml().Leer(archivo, ModoSeparacionRutas.COLUMNA_NUMERO_RUTA, null);

        Assert.Empty(resultado.Errores);
        // La ruta 7 aparece en dos bloques: son dos rutas (doble ruta del mismo vehiculo), no una de 2 pasajeros.
        Assert.Equal(new[] { "1", "7", "12", "7 (bloque 2)" }, resultado.Rutas.Select(r => r.Referencia));
        var primera = resultado.Rutas[1];
        Assert.Equal(new[] { 52192462L }, primera.Filas.Select(f => long.Parse(f.Cedula)));
        Assert.Equal(new[] { 30237530L }, resultado.Rutas[3].Filas.Select(f => long.Parse(f.Cedula)));
        Assert.Equal("USE532", primera.Placa);
        Assert.Equal(new TimeOnly(6, 0), primera.Hora);
        Assert.Equal(TipoServicio.ENTRADA, primera.Tipo);
        Assert.Equal(new DateOnly(2026, 12, 27), primera.Fecha);
        Assert.Equal(2, resultado.Rutas[0].Filas.Count);
    }

    [Fact]
    public void PorNumeroDeRuta_FilasSeguidasConElMismoNumeroSonUnaSolaRuta()
    {
        using var archivo = Libro(hoja =>
        {
            Encabezado(hoja, 3);
            Pasajero(hoja, 5, 1057787629, "Dahiana Salazar", 1, "STQ789");
            Pasajero(hoja, 6, 1059812320, "Yuliana Marin", 1, "STQ789");
            Pasajero(hoja, 8, 52192462, "Omaira Hernandez", 1, "STQ789");
        });

        var resultado = new ExcelRutasArmadasLectorClosedXml().Leer(archivo, ModoSeparacionRutas.COLUMNA_NUMERO_RUTA, null);

        // Una fila en blanco no corta el bloque: solo lo cortan las lineas con texto que no son de pasajero.
        Assert.Single(resultado.Rutas);
        Assert.Equal(3, resultado.Rutas[0].Filas.Count);
    }

    [Fact]
    public void ConTelefonoYCelular_ElCelularRealGanaAlTelefonoEnCero()
    {
        using var archivo = Libro(hoja =>
        {
            Encabezado(hoja, 3);
            Pasajero(hoja, 5, 1057787629, "Dahiana Salazar", 1, "STQ789");
            Poner(hoja, 5, (6, 0), (7, 3117924418L));
            Pasajero(hoja, 6, 1059812320, "Yuliana Marin", 1, "STQ789");
            Poner(hoja, 6, (6, 8915645L), (7, 3146261314L));
            Pasajero(hoja, 7, 30334408, "Diana Colorado", 1, "STQ789");
            Poner(hoja, 7, (6, 0), (7, 0));
        });

        var resultado = new ExcelRutasArmadasLectorClosedXml().Leer(archivo, ModoSeparacionRutas.COLUMNA_NUMERO_RUTA, null);

        // Hay una columna TELEFONO (casi siempre en 0 o con un fijo) y otra CELULAR: se toma el celular que sirva.
        Assert.Equal(new[] { "3117924418", "3146261314", string.Empty }, resultado.Rutas[0].Filas.Select(f => f.Celular));
    }

    [Fact]
    public void PorNumeroDeRuta_SinColumnaDeRuta_PideLaColumna()
    {
        using var archivo = Libro(hoja =>
        {
            Poner(hoja, 1, (1, "Cedula"), (2, "Nombre"), (3, "Direccion"), (4, "Barrio"), (5, "Hora entrada"));
            Poner(hoja, 2, (1, 1053764960L), (2, "Luz"), (3, "Calle 1"), (4, "Centro"), (5, new TimeSpan(5, 0, 0)));
        });

        var resultado = new ExcelRutasArmadasLectorClosedXml().Leer(archivo, ModoSeparacionRutas.COLUMNA_NUMERO_RUTA, null);

        Assert.Contains(resultado.Errores, e => e.Contains("número de ruta"));
    }

    [Fact]
    public void PorLineaDeInicio_CadaLineaAbreUnaRutaConLosDatosQueTraiga()
    {
        using var archivo = Libro(hoja =>
        {
            Poner(hoja, 1, (1, "Cedula"), (2, "Nombre"), (3, "Direccion"), (4, "Barrio"), (5, "Celular"));
            Poner(hoja, 2, (1, "RUTA 1 - ENTRADA 5:00 am - STQ789"));
            Poner(hoja, 3, (1, 1053764960L), (2, "Luz Matinez"), (3, "Calle 1"), (4, "Centro"));
            Poner(hoja, 4, (1, 1053764961L), (2, "Ana Gomez"), (3, "Calle 2"), (4, "Prado"));
            Poner(hoja, 5, (1, "Ruta 2 salida 4:30 pm"));
            Poner(hoja, 6, (1, 1053764962L), (2, "Pedro Rios"), (3, "Calle 3"), (4, "Suba"));
        });

        var resultado = new ExcelRutasArmadasLectorClosedXml().Leer(archivo, ModoSeparacionRutas.LINEA_INICIO, "ruta");

        Assert.Empty(resultado.Errores);
        Assert.Equal(2, resultado.Rutas.Count);
        Assert.Equal((TipoServicio.ENTRADA, new TimeOnly(5, 0), "STQ789", 2), (resultado.Rutas[0].Tipo, resultado.Rutas[0].Hora, resultado.Rutas[0].Placa, resultado.Rutas[0].Filas.Count));
        Assert.Equal((TipoServicio.SALIDA, new TimeOnly(16, 30), string.Empty, 1), (resultado.Rutas[1].Tipo, resultado.Rutas[1].Hora, resultado.Rutas[1].Placa, resultado.Rutas[1].Filas.Count));
    }

    [Fact]
    public void PorLineaDeInicio_PasajerosAntesDeLaPrimeraLinea_EsError()
    {
        using var archivo = Libro(hoja =>
        {
            Poner(hoja, 1, (1, "Cedula"), (2, "Nombre"), (3, "Direccion"), (4, "Barrio"), (5, "Hora entrada"));
            Poner(hoja, 2, (1, 1053764960L), (2, "Luz"), (3, "Calle 1"), (4, "Centro"), (5, new TimeSpan(5, 0, 0)));
        });

        var resultado = new ExcelRutasArmadasLectorClosedXml().Leer(archivo, ModoSeparacionRutas.LINEA_INICIO, "RUTA");

        Assert.Contains(resultado.Errores, e => e.Contains("antes de la primera línea"));
    }

    [Fact]
    public void RutaConHorasDistintasEnSusFilas_EsError()
    {
        using var archivo = Libro(hoja =>
        {
            Encabezado(hoja, 3);
            Pasajero(hoja, 5, 1057787629, "Dahiana Salazar", 1, "STQ789", hora: 5);
            Pasajero(hoja, 6, 1059812320, "Yuliana Marin", 1, "STQ789", hora: 6);
        });

        var resultado = new ExcelRutasArmadasLectorClosedXml().Leer(archivo, ModoSeparacionRutas.COLUMNA_NUMERO_RUTA, null);

        Assert.Contains(resultado.Errores, e => e.Contains("horario no coincide"));
    }

    [Fact]
    public void NombresConCaracteresDanados_SeCorrigen()
    {
        using var archivo = Libro(hoja =>
        {
            Encabezado(hoja, 3);
            Pasajero(hoja, 5, 1056303328, "Cristian Camilo Henao Casta�o", 1, "STQ789");
            Pasajero(hoja, 6, 30400642, "Liliana P�rez Serna", 1, "STQ789");
        });

        var resultado = new ExcelRutasArmadasLectorClosedXml().Leer(archivo, ModoSeparacionRutas.COLUMNA_NUMERO_RUTA, null);

        Assert.Equal(new[] { "Cristian Camilo Henao Castaño", "Liliana Pérez Serna" }, resultado.Rutas[0].Filas.Select(f => f.NombreCompleto));
        Assert.Empty(resultado.Advertencias);
    }
}
