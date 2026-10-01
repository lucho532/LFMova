using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.UnitTests.Domain.Rules;

public class ReglasEstadoServicioPasajeroTests
{
    [Fact]
    public void EsTransicionDeResultadoValida_SoloPermiteRecogerDesdeLaLlegada()
    {
        Assert.True(ReglasEstadoServicioPasajero.EsTransicionDeResultadoValida(EstadoServicioPasajero.CONDUCTOR_LLEGO, EstadoServicioPasajero.RECOGIDO));
        Assert.False(ReglasEstadoServicioPasajero.EsTransicionDeResultadoValida(EstadoServicioPasajero.PROGRAMADO, EstadoServicioPasajero.RECOGIDO));
        Assert.False(ReglasEstadoServicioPasajero.EsTransicionDeResultadoValida(EstadoServicioPasajero.CONDUCTOR_LLEGO, EstadoServicioPasajero.NO_RECOGIDO));
        Assert.False(ReglasEstadoServicioPasajero.EsTransicionDeResultadoValida(EstadoServicioPasajero.RECOGIDO, EstadoServicioPasajero.CONDUCTOR_LLEGO));
    }

    [Theory]
    [InlineData(TipoIncidencia.NO_CONTESTA, true)]
    [InlineData(TipoIncidencia.NO_SE_ENCUENTRA, true)]
    [InlineData(TipoIncidencia.DIRECCION_INCORRECTA, true)]
    [InlineData(TipoIncidencia.NO_SE_PUDO_RECOGER, true)]
    [InlineData(TipoIncidencia.UBICACION_MODIFICADA, false)]
    [InlineData(TipoIncidencia.OTRA, false)]
    public void IncidenciaImpideLaRecogida_DevuelveElValorEsperado(TipoIncidencia tipo, bool esperado)
    {
        Assert.Equal(esperado, ReglasEstadoServicioPasajero.IncidenciaImpideLaRecogida(tipo));
    }

    [Theory]
    [InlineData(EstadoServicioPasajero.PROGRAMADO, true)]
    [InlineData(EstadoServicioPasajero.CONFIRMADO, true)]
    [InlineData(EstadoServicioPasajero.CONDUCTOR_LLEGO, true)]
    [InlineData(EstadoServicioPasajero.RECOGIDO, false)]
    [InlineData(EstadoServicioPasajero.NO_RECOGIDO, false)]
    [InlineData(EstadoServicioPasajero.NO_ASISTIRA, false)]
    public void PuedeQuedarNoRecogido_SoloSiAunNoTieneResultado(EstadoServicioPasajero estado, bool esperado)
    {
        Assert.Equal(esperado, ReglasEstadoServicioPasajero.PuedeQuedarNoRecogido(estado));
    }

    [Theory]
    [InlineData(EstadoServicioPasajero.RECOGIDO, true)]
    [InlineData(EstadoServicioPasajero.NO_RECOGIDO, true)]
    [InlineData(EstadoServicioPasajero.NO_ASISTIRA, true)]
    [InlineData(EstadoServicioPasajero.CANCELADO, true)]
    [InlineData(EstadoServicioPasajero.PROGRAMADO, false)]
    [InlineData(EstadoServicioPasajero.CONFIRMADO, false)]
    [InlineData(EstadoServicioPasajero.CONDUCTOR_LLEGO, false)]
    public void EstaProcesado_DevuelveElValorEsperado(EstadoServicioPasajero estado, bool esperado)
    {
        Assert.Equal(esperado, ReglasEstadoServicioPasajero.EstaProcesado(estado));
    }
}
