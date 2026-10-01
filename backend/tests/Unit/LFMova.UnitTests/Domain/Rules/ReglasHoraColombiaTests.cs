using LFMova.Domain.Rules;

namespace LFMova.UnitTests.Domain.Rules;

public class ReglasHoraColombiaTests
{
    [Fact]
    public void AhoraColombia_RestaCincoHoras_InclusoCruzandoLaMedianoche()
    {
        var ahoraUtc = new DateTime(2026, 9, 30, 3, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 9, 29, 22, 0, 0), ReglasHoraColombia.AhoraColombia(ahoraUtc));
    }

    [Fact]
    public void AlertaDeUnaRutaDeLaUna_SeActivaDesdeLaMedianocheDeColombia()
    {
        // Ruta de entrada a la 01:00 a. m. del 30/09: a las 00:30 de Colombia (05:30 UTC) sí toca avisar.
        var ahoraColombia = ReglasHoraColombia.AhoraColombia(new DateTime(2026, 9, 30, 5, 30, 0, DateTimeKind.Utc));

        Assert.True(ReglasAlertaEjecucion.DebeAlertarRutaNoIniciada(new DateOnly(2026, 9, 30), new TimeOnly(1, 0), ahoraColombia));
    }
}
