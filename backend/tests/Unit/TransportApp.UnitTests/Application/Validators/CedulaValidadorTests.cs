using TransportApp.Application.Validators;

namespace TransportApp.UnitTests.Application.Validators;

public class CedulaValidadorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EsValida_DevuelveFalso_CuandoLaCedulaEstaVacia(string? cedula)
    {
        Assert.False(CedulaValidador.EsValida(cedula!));
    }

    [Fact]
    public void EsValida_DevuelveVerdadero_CuandoLaCedulaTieneContenido()
    {
        Assert.True(CedulaValidador.EsValida("123456789"));
    }
}
