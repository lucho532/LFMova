using LFMova.Application.Validators;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Validators;

public class TipoServicioValidadorTests
{
    [Theory]
    [InlineData(TipoServicio.ENTRADA)]
    [InlineData(TipoServicio.SALIDA)]
    public void EsValido_DevuelveVerdadero_ParaValoresDefinidos(TipoServicio tipo)
    {
        Assert.True(TipoServicioValidador.EsValido(tipo));
    }

    [Fact]
    public void EsValido_DevuelveFalso_ParaUnValorFueraDeRango()
    {
        Assert.False(TipoServicioValidador.EsValido((TipoServicio)99));
    }
}
