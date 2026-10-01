using TransportApp.Domain.Rules;

namespace TransportApp.UnitTests.Domain.Rules;

public class ReglasNomenclaturaVillamariaTests
{
    [Theory]
    [InlineData("CALLE 10 A1 # 38 A-20")] // el caso real: carrera 38 no existe en el casco urbano
    [InlineData("Carrera 38 # 10-20")]
    [InlineData("CRA 38 N° 10A-15")]
    [InlineData("Calle 80 # 25-10")]      // ni en el casco urbano ni en el sector oriental
    public void DireccionPuedeSerDeVillamaria_EsFalso_CuandoElCruceNoExisteEnVillamaria(string direccion)
    {
        Assert.False(ReglasNomenclaturaVillamaria.DireccionPuedeSerDeVillamaria(direccion));
    }

    [Theory]
    [InlineData("Calle 5 # 8-20")]
    [InlineData("Carrera 10 # 5-30")]
    [InlineData("Cl 8 #15-2")]
    [InlineData("CALLE 22 NO 20-10")]
    [InlineData("Calle 65 # 36-10")]      // sector oriental, con la numeración de Manizales
    public void DireccionPuedeSerDeVillamaria_EsVerdadero_CuandoElCruceExisteEnVillamaria(string direccion)
    {
        Assert.True(ReglasNomenclaturaVillamaria.DireccionPuedeSerDeVillamaria(direccion));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Manzana 3 casa 4")]
    [InlineData("Vereda La Linda")]
    public void DireccionPuedeSerDeVillamaria_EsNulo_CuandoNoSePuedeSaber(string? direccion)
    {
        Assert.Null(ReglasNomenclaturaVillamaria.DireccionPuedeSerDeVillamaria(direccion));
    }
}
