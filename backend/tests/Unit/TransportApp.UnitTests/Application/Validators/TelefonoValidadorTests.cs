using TransportApp.Application.Validators;

namespace TransportApp.UnitTests.Application.Validators;

public class TelefonoValidadorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EsValido_DevuelveFalso_CuandoElTelefonoEstaVacio(string? telefono)
    {
        Assert.False(TelefonoValidador.EsValido(telefono!));
    }

    [Fact]
    public void EsValido_DevuelveVerdadero_CuandoElTelefonoTieneContenido()
    {
        Assert.True(TelefonoValidador.EsValido("3000000000"));
    }
}
