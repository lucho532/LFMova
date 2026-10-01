using TransportApp.Application.Validators;

namespace TransportApp.UnitTests.Application.Validators;

public class CapacidadValidadorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EsValida_DevuelveFalso_CuandoLaCapacidadNoEsPositiva(int capacidad)
    {
        Assert.False(CapacidadValidador.EsValida(capacidad));
    }

    [Fact]
    public void EsValida_DevuelveVerdadero_CuandoLaCapacidadEsPositiva()
    {
        Assert.True(CapacidadValidador.EsValida(4));
    }
}
