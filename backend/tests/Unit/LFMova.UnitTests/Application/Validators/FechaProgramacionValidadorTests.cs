using LFMova.Application.Validators;

namespace LFMova.UnitTests.Application.Validators;

public class FechaProgramacionValidadorTests
{
    [Fact]
    public void EsValida_DevuelveFalso_CuandoLaFechaEsElValorPorDefecto()
    {
        Assert.False(FechaProgramacionValidador.EsValida(default));
    }

    [Fact]
    public void EsValida_DevuelveVerdadero_CuandoLaFechaFueEstablecida()
    {
        Assert.True(FechaProgramacionValidador.EsValida(new DateOnly(2026, 1, 20)));
    }
}
