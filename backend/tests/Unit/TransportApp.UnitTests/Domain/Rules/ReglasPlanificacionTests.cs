using TransportApp.Domain.Entities;
using TransportApp.Domain.Rules;

namespace TransportApp.UnitTests.Domain.Rules;

public class ReglasPlanificacionTests
{
    [Fact]
    public void CalcularDistanciaKm_DevuelveCero_CuandoLasCoordenadasSonIguales()
    {
        var distancia = ReglasPlanificacion.CalcularDistanciaKm(4.6, -74.1, 4.6, -74.1);

        Assert.Equal(0, distancia!.Value, 3);
    }

    [Fact]
    public void CalcularDistanciaKm_DevuelveNulo_CuandoFaltaAlgunaCoordenada()
    {
        Assert.Null(ReglasPlanificacion.CalcularDistanciaKm(null, -74.1, 4.6, -74.1));
        Assert.Null(ReglasPlanificacion.CalcularDistanciaKm(4.6, null, 4.6, -74.1));
        Assert.Null(ReglasPlanificacion.CalcularDistanciaKm(4.6, -74.1, null, -74.1));
        Assert.Null(ReglasPlanificacion.CalcularDistanciaKm(4.6, -74.1, 4.6, null));
    }

    [Fact]
    public void CalcularDistanciaKm_CalculaUnaDistanciaAproximadaRazonable()
    {
        // Bogotá (4.6097, -74.0817) a Chía (4.8600, -74.0300): ~29 km en línea recta.
        var distancia = ReglasPlanificacion.CalcularDistanciaKm(4.6097, -74.0817, 4.8600, -74.0300);

        Assert.InRange(distancia!.Value, 25, 33);
    }

    [Fact]
    public void ExcedeCapacidad_DevuelveVerdadero_CuandoHayMasPasajerosQueCapacidad()
    {
        Assert.True(ReglasPlanificacion.ExcedeCapacidad(5, 4));
    }

    [Fact]
    public void ExcedeCapacidad_DevuelveFalso_CuandoLaCantidadEsIgualOMenor()
    {
        Assert.False(ReglasPlanificacion.ExcedeCapacidad(4, 4));
        Assert.False(ReglasPlanificacion.ExcedeCapacidad(3, 4));
    }

    [Fact]
    public void OrdenarParaEntrada_OrdenaDelMasLejanoAlMasCercanoDeLaSede()
    {
        // Sede en (0,0). Cercano a 1 grado (~111km), lejano a 3 grados (~333km).
        var cercano = new ServicioPasajero { ServicioPasajeroId = 1, Latitud = 1, Longitud = 0 };
        var lejano = new ServicioPasajero { ServicioPasajeroId = 2, Latitud = 3, Longitud = 0 };
        var intermedio = new ServicioPasajero { ServicioPasajeroId = 3, Latitud = 2, Longitud = 0 };

        var ordenado = ReglasPlanificacion.OrdenarParaEntrada([cercano, lejano, intermedio], sedeLatitud: 0, sedeLongitud: 0);

        Assert.Equal([2, 3, 1], ordenado.Select(p => p.ServicioPasajeroId));
    }

    [Fact]
    public void OrdenarParaEntrada_UbicaAlFinalALosPasajerosSinCoordenadas_ConservandoSuOrdenOriginal()
    {
        var sinCoordenadas1 = new ServicioPasajero { ServicioPasajeroId = 1, Latitud = null, Longitud = null };
        var conCoordenadas = new ServicioPasajero { ServicioPasajeroId = 2, Latitud = 1, Longitud = 0 };
        var sinCoordenadas2 = new ServicioPasajero { ServicioPasajeroId = 3, Latitud = null, Longitud = null };

        var ordenado = ReglasPlanificacion.OrdenarParaEntrada(
            [sinCoordenadas1, conCoordenadas, sinCoordenadas2], sedeLatitud: 0, sedeLongitud: 0);

        Assert.Equal([2, 1, 3], ordenado.Select(p => p.ServicioPasajeroId));
    }

    [Fact]
    public void HoraLimiteLlegadaSede_RestaLosMinutosDeAnticipacion()
    {
        var horaLimite = ReglasPlanificacion.HoraLimiteLlegadaSede(new TimeOnly(8, 0));

        Assert.Equal(new TimeOnly(7, 45), horaLimite);
    }

    [Fact]
    public void HoraSugeridaInicioRecogida_RestaLaVentanaDeRecogidaALaHoraLimite()
    {
        var horaSugerida = ReglasPlanificacion.HoraSugeridaInicioRecogida(new TimeOnly(7, 45), ventanaRecogidaMinutos: 25);

        Assert.Equal(new TimeOnly(7, 20), horaSugerida);
    }
}
