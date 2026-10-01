using TransportApp.Application.Utils;

namespace TransportApp.UnitTests.Application.Utils;

public class CorreoNormalizadorTests
{
    [Theory]
    [InlineData("Persona8@Example.com", "persona8@example.com")]
    [InlineData("  ANA@Correo.COM ", "ana@correo.com")]
    [InlineData("ya.normal@test.com", "ya.normal@test.com")]
    public void Normalizar_QuitaEspaciosYPasaAMinusculas(string correo, string esperado)
    {
        Assert.Equal(esperado, CorreoNormalizador.Normalizar(correo));
    }
}
