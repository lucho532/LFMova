using TransportApp.Domain.Enums;
using TransportApp.Domain.Rules;

namespace TransportApp.UnitTests.Domain.Rules;

public class ReglasEstadoServicioTests
{
    [Theory]
    [InlineData(EstadoServicio.BORRADOR, EstadoServicio.PENDIENTE_ASIGNACION, true)]
    [InlineData(EstadoServicio.BORRADOR, EstadoServicio.CANCELADO, true)]
    [InlineData(EstadoServicio.BORRADOR, EstadoServicio.ASIGNADO, false)]
    [InlineData(EstadoServicio.PENDIENTE_ASIGNACION, EstadoServicio.ASIGNADO, true)]
    [InlineData(EstadoServicio.ASIGNADO, EstadoServicio.PUBLICADO, true)]
    [InlineData(EstadoServicio.ASIGNADO, EstadoServicio.PENDIENTE_ASIGNACION, true)]
    [InlineData(EstadoServicio.PUBLICADO, EstadoServicio.EN_CURSO, true)]
    [InlineData(EstadoServicio.PUBLICADO, EstadoServicio.ASIGNADO, true)]
    [InlineData(EstadoServicio.PUBLICADO, EstadoServicio.PENDIENTE_ASIGNACION, false)]
    [InlineData(EstadoServicio.EN_CURSO, EstadoServicio.FINALIZADO, true)]
    [InlineData(EstadoServicio.EN_CURSO, EstadoServicio.CANCELADO, false)]
    [InlineData(EstadoServicio.FINALIZADO, EstadoServicio.EN_CURSO, false)]
    [InlineData(EstadoServicio.CANCELADO, EstadoServicio.BORRADOR, false)]
    public void EsTransicionValida_DevuelveElResultadoEsperado(EstadoServicio actual, EstadoServicio nuevo, bool esperado)
    {
        Assert.Equal(esperado, ReglasEstadoServicio.EsTransicionValida(actual, nuevo));
    }
}
