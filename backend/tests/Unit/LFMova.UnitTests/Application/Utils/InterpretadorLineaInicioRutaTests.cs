using LFMova.Application.Utils;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Utils;

public class InterpretadorLineaInicioRutaTests
{
    [Theory]
    [InlineData("RUTA 1 - ENTRADA 5:00 am - STQ789", 5, 0, "STQ789", TipoServicio.ENTRADA)]
    [InlineData("Ruta 2 salida 4:30 pm", 16, 30, null, TipoServicio.SALIDA)]
    [InlineData("RUTA 3 06:00 placa stq-789", 6, 0, "STQ789", null)]
    [InlineData("Ruta 4 - 5 am", 5, 0, null, null)]
    [InlineData("RUTA 5 17.30 USE532 SALIDA", 17, 30, "USE532", TipoServicio.SALIDA)]
    public void Interpretar_TomaHoraPlacaYTipoCuandoLaLineaLosTraiga(string linea, int hora, int minuto, string? placa, TipoServicio? tipo)
    {
        var datos = InterpretadorLineaInicioRuta.Interpretar(linea);

        Assert.Equal(new TimeOnly(hora, minuto), datos.Hora);
        Assert.Equal(placa, datos.Placa);
        Assert.Equal(tipo, datos.Tipo);
    }

    [Fact]
    public void Interpretar_UnNumeroDeRutaNoEsUnaHora()
    {
        var datos = InterpretadorLineaInicioRuta.Interpretar("RUTA 12");

        Assert.Null(datos.Hora);
        Assert.Null(datos.Placa);
        Assert.Null(datos.Tipo);
    }
}
