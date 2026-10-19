using LFMova.Application.Utils;

namespace LFMova.UnitTests.Application.Utils;

public class SeleccionadorTelefonoTests
{
    [Theory]
    [InlineData("3117924418", true)]
    [InlineData("8915645", true)]
    [InlineData("0", false)]
    [InlineData("0000000", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void EsUtil_ExigeSieteDigitosYQueNoSeanSoloCeros(string? telefono, bool esperado)
    {
        Assert.Equal(esperado, SeleccionadorTelefono.EsUtil(telefono));
    }

    [Fact]
    public void ElegirMejor_PrefiereElCelularSobreUnFijoYDescartaLosCeros()
    {
        Assert.Equal("3117924418", SeleccionadorTelefono.ElegirMejor(new[] { "0", "3117924418" }));
        Assert.Equal("3117924418", SeleccionadorTelefono.ElegirMejor(new[] { "8915645", "3117924418" }));
    }

    [Fact]
    public void ElegirMejor_SiNoHayCelularUsaElFijoYSiNadaSirveDevuelveVacio()
    {
        Assert.Equal("8915645", SeleccionadorTelefono.ElegirMejor(new[] { "0", "8915645" }));
        Assert.Equal(string.Empty, SeleccionadorTelefono.ElegirMejor(new[] { "0", "" }));
    }

    [Theory]
    [InlineData("8915645", "3227001122", true)]     // un fijo se cambia por un celular
    [InlineData("0", "3227001122", true)]           // un cero también
    [InlineData("", "3227001122", true)]
    [InlineData("3117924418", "3227001122", true)]  // el empleado cambió de celular
    [InlineData("8915645", "8928691", true)]        // y de fijo
    [InlineData("3117924418", "3117924418", false)] // el mismo número no es un cambio
    [InlineData("3117924418", "311 792 4418", false)]
    [InlineData("3117924418", "8915645", false)]    // un archivo que solo trae el fijo no quita el celular
    [InlineData("8915645", "0", false)]             // ni un cero reemplaza nada
    [InlineData("3117924418", "", false)]
    public void DebeReemplazar_ActualizaLosCambiosPeroNuncaPierdeUnCelularPorUnFijo(string actual, string nuevo, bool esperado)
    {
        Assert.Equal(esperado, SeleccionadorTelefono.DebeReemplazar(actual, nuevo));
    }
}
