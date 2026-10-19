using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.UnitTests.Domain.Rules;

/// <summary>Pruebas de qué estados cuentan como asistencia y del orden cronológico de la ruta.</summary>
public class ReglasAsistenciaRutaTests
{
    [Theory]
    [InlineData(EstadoServicioPasajero.RECOGIDO, FiltroAsistenciaRuta.ASISTIERON, true)]
    [InlineData(EstadoServicioPasajero.NO_RECOGIDO, FiltroAsistenciaRuta.ASISTIERON, false)]
    [InlineData(EstadoServicioPasajero.NO_ASISTIRA, FiltroAsistenciaRuta.ASISTIERON, false)]
    [InlineData(EstadoServicioPasajero.NO_RECOGIDO, FiltroAsistenciaRuta.NO_ASISTIERON, true)]
    [InlineData(EstadoServicioPasajero.NO_ASISTIRA, FiltroAsistenciaRuta.NO_ASISTIERON, true)]
    [InlineData(EstadoServicioPasajero.RECOGIDO, FiltroAsistenciaRuta.NO_ASISTIERON, false)]
    [InlineData(EstadoServicioPasajero.PROGRAMADO, FiltroAsistenciaRuta.NO_ASISTIERON, false)]
    [InlineData(EstadoServicioPasajero.CANCELADO, FiltroAsistenciaRuta.ASISTIERON, false)]
    [InlineData(EstadoServicioPasajero.CANCELADO, FiltroAsistenciaRuta.TODOS, true)]
    [InlineData(EstadoServicioPasajero.RECOGIDO, FiltroAsistenciaRuta.TODOS, true)]
    public void Cumple_SegunElEstadoYElFiltro(EstadoServicioPasajero estado, FiltroAsistenciaRuta filtro, bool esperado)
    {
        Assert.Equal(esperado, ReglasAsistenciaRuta.Cumple(estado, filtro));
    }

    [Fact]
    public void OrdenarCronologicamente_PoneAntesLosProcesados_PorHora_YDespuesLosDemas_PorOrden()
    {
        var pasajeros = new[]
        {
            new ServicioPasajero { ServicioPasajeroId = 1, Orden = 1 },
            new ServicioPasajero { ServicioPasajeroId = 2, Orden = 2, HoraProcesado = new DateTime(2026, 1, 1, 10, 5, 0) },
            new ServicioPasajero { ServicioPasajeroId = 3, Orden = 3, HoraProcesado = new DateTime(2026, 1, 1, 10, 1, 0) },
            new ServicioPasajero { ServicioPasajeroId = 4, Orden = 0 },
        };

        var ordenados = ReglasAsistenciaRuta.OrdenarCronologicamente(pasajeros);

        Assert.Equal(new[] { 3, 2, 4, 1 }, ordenados.Select(p => p.ServicioPasajeroId));
    }
}
