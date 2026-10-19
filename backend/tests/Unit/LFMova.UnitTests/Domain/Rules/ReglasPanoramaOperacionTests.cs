using LFMova.Domain.Rules;

namespace LFMova.UnitTests.Domain.Rules;

public class ReglasPanoramaOperacionTests
{
    [Theory]
    [InlineData(1084, 1000, 8.4)]
    [InlineData(500, 1000, -50.0)]
    [InlineData(100, 100, 0.0)]
    public void VariacionPorcentual_CalculaElCambioFrenteAlPeriodoAnterior(int actual, int anterior, double esperado)
    {
        Assert.Equal(esperado, ReglasPanoramaOperacion.VariacionPorcentual(actual, anterior));
    }

    [Fact]
    public void VariacionPorcentual_EsNulaCuandoElPeriodoAnteriorNoTuvoPasajeros()
    {
        Assert.Null(ReglasPanoramaOperacion.VariacionPorcentual(40, 0));
    }

    [Theory]
    [InlineData(90, 10, 90.0)]
    [InlineData(2, 1, 66.7)]
    [InlineData(0, 5, 0.0)]
    public void PromedioAsistencia_EsElPorcentajeDeQuienesAsistieron(int asistieron, int noAsistieron, double esperado)
    {
        Assert.Equal(esperado, ReglasPanoramaOperacion.PromedioAsistencia(asistieron, noAsistieron));
    }

    [Fact]
    public void PromedioAsistencia_EsCeroSinPasajeros()
    {
        Assert.Equal(0, ReglasPanoramaOperacion.PromedioAsistencia(0, 0));
    }
}
