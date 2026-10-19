using LFMova.Application.Utils;

namespace LFMova.UnitTests.Application.Utils;

public class CorrectorTextoDanadoTests
{
    [Theory]
    [InlineData("Casta�o", "Castaño")]
    [InlineData("Londo�o Vargas", "Londoño Vargas")]
    [InlineData("Lully Smit Tabares Mu�oz", "Lully Smit Tabares Muñoz")]
    [InlineData("P�rez", "Pérez")]
    [InlineData("Rodr�guez", "Rodríguez")]
    [InlineData("RODR�GUEZ", "RODRÍGUEZ")]
    [InlineData("Mar�a", "María")]
    [InlineData("detr�s del �xito", "detrás del éxito")]
    public void Corregir_RecuperaLetrasConTildeYEnie(string danado, string esperado)
    {
        Assert.Equal(esperado, CorrectorTextoDanado.Corregir(danado, out var sinResolver));
        Assert.False(sinResolver);
    }

    [Fact]
    public void Corregir_UnCaracterDanadoPegadoAOtraPalabraEsUnEspacio()
    {
        Assert.Equal("Claudia Viviana Rodriguez Herrera", CorrectorTextoDanado.Corregir("Claudia�Viviana� Rodriguez� Herrera", out var sinResolver));
        Assert.False(sinResolver);
    }

    [Fact]
    public void Corregir_DeshaceLaDobleCodificacion()
    {
        Assert.Equal("Castaño", CorrectorTextoDanado.Corregir("CastaÃ±o", out _));
    }

    [Fact]
    public void Corregir_SiNoPuedeResolverUnaPalabraLoInforma()
    {
        CorrectorTextoDanado.Corregir("Xyz�bc", out var sinResolver);

        Assert.True(sinResolver);
    }

    [Fact]
    public void Corregir_QuitaEspaciosDuplicadosYCaracteresInvisibles()
    {
        Assert.Equal("Claudia Viviana Rodriguez", CorrectorTextoDanado.Corregir("Claudia Viviana  Rodriguez​ ", out _));
    }

    [Fact]
    public void Corregir_NoCambiaUnTextoSano()
    {
        Assert.Equal("María Pérez Muñoz", CorrectorTextoDanado.Corregir("María Pérez Muñoz", out var sinResolver));
        Assert.False(sinResolver);
    }
}
