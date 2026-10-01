using TransportApp.Application.Utils;

namespace TransportApp.UnitTests.Application.Utils;

public class FormatoOperacionTests
{
    [Theory]
    [InlineData(1, 0, "01:00 a. m.")]
    [InlineData(0, 15, "12:15 a. m.")]
    [InlineData(12, 0, "12:00 p. m.")]
    [InlineData(13, 30, "01:30 p. m.")]
    [InlineData(22, 5, "10:05 p. m.")]
    public void Hora_UsaFormatoDeDoceHoras(int hora, int minuto, string esperado)
    {
        Assert.Equal(esperado, FormatoOperacion.Hora(new TimeOnly(hora, minuto)));
    }

    [Theory]
    [InlineData(1, "la 01:00 a. m.")]
    [InlineData(13, "la 01:00 p. m.")]
    [InlineData(6, "las 06:00 a. m.")]
    [InlineData(0, "las 12:00 a. m.")]
    public void HoraConArticulo_UsaLaSoloParaLaUna(int hora, string esperado)
    {
        Assert.Equal(esperado, FormatoOperacion.HoraConArticulo(new TimeOnly(hora, 0)));
    }

    [Theory]
    [InlineData("andres felipe otalvaro franco", "Andres Felipe Otalvaro Franco")]
    [InlineData("  MARÍA   josé ", "María José")]
    public void NombrePropio_PoneMayusculaInicialEnCadaPalabra(string nombre, string esperado)
    {
        Assert.Equal(esperado, FormatoOperacion.NombrePropio(nombre));
    }
}
