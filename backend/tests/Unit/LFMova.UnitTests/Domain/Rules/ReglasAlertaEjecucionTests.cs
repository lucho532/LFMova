using LFMova.Domain.Rules;

namespace LFMova.UnitTests.Domain.Rules;

public class ReglasAlertaEjecucionTests
{
    private static readonly DateOnly Fecha = new(2026, 1, 20);
    private static readonly TimeOnly HoraProgramada = new(8, 0);

    [Theory]
    [InlineData(7, 0)]   // falta exactamente una hora
    [InlineData(7, 45)]  // faltan 15 minutos
    [InlineData(7, 59)]  // falta un minuto
    public void DebeAlertarRutaNoIniciada_AvisaDesdeUnaHoraAntesHastaLaHoraDeEntrada(int hora, int minuto)
    {
        var ahoraColombia = new DateTime(2026, 1, 20, hora, minuto, 0);

        Assert.True(ReglasAlertaEjecucion.DebeAlertarRutaNoIniciada(Fecha, HoraProgramada, ahoraColombia));
    }

    [Theory]
    [InlineData(6, 59)]  // falta más de una hora
    [InlineData(8, 0)]   // ya es la hora de entrada: ese horario quedó atrás
    [InlineData(9, 0)]   // ya pasó la hora de entrada
    public void DebeAlertarRutaNoIniciada_NoAvisaAntesDeLaVentanaNiDesdeLaHoraDeEntrada(int hora, int minuto)
    {
        var ahoraColombia = new DateTime(2026, 1, 20, hora, minuto, 0);

        Assert.False(ReglasAlertaEjecucion.DebeAlertarRutaNoIniciada(Fecha, HoraProgramada, ahoraColombia));
    }

    [Theory]
    [InlineData(7, 30, 30)]
    [InlineData(8, 0, 0)]
    [InlineData(8, 20, -20)]
    public void MinutosParaInicio_EsPositivoAntesYNegativoConRetraso(int hora, int minuto, int esperado)
    {
        Assert.Equal(esperado, ReglasAlertaEjecucion.MinutosParaInicio(Fecha, HoraProgramada, new DateTime(2026, 1, 20, hora, minuto, 0)));
    }
}
