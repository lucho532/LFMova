using LFMova.Application.Utils;

namespace LFMova.UnitTests.Application.Utils;

public class HasheadorContrasenasTests
{
    private readonly HasheadorContrasenas _hasheador = new();

    [Fact]
    public void Verificar_DevuelveVerdadero_CuandoLaContrasenaEsCorrecta()
    {
        var hash = _hasheador.Hashear("MiContrasena123");

        Assert.True(_hasheador.Verificar("MiContrasena123", hash));
    }

    [Fact]
    public void Verificar_DevuelveFalso_CuandoLaContrasenaEsIncorrecta()
    {
        var hash = _hasheador.Hashear("MiContrasena123");

        Assert.False(_hasheador.Verificar("OtraContrasena", hash));
    }

    [Fact]
    public void Hashear_GeneraHashesDistintos_ParaLaMismaContrasena()
    {
        var hash1 = _hasheador.Hashear("MiContrasena123");
        var hash2 = _hasheador.Hashear("MiContrasena123");

        Assert.NotEqual(hash1, hash2);
    }
}
