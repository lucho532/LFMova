using TransportApp.Domain.Entities;
using TransportApp.Domain.Rules;

namespace TransportApp.UnitTests.Domain.Rules;

public class ReglasBarrerasGeograficasTests
{
    private static BarreraGeografica Barrera(string barrioA, string barrioB) =>
        new() { EmpresaId = 1, BarrioA = barrioA, BarrioB = barrioB };

    [Fact]
    public void HayBarreraEntre_DevuelveVerdadero_CuandoElParCoincideEnElMismoOrden()
    {
        var barreras = new[] { Barrera("La Linda", "La Francia") };

        Assert.True(ReglasBarrerasGeograficas.HayBarreraEntre(barreras, "La Linda", "La Francia"));
    }

    [Fact]
    public void HayBarreraEntre_DevuelveVerdadero_CuandoElParEstaInvertido()
    {
        var barreras = new[] { Barrera("La Linda", "La Francia") };

        Assert.True(ReglasBarrerasGeograficas.HayBarreraEntre(barreras, "La Francia", "La Linda"));
    }

    [Fact]
    public void HayBarreraEntre_IgnoraMayusculasYTildes()
    {
        var barreras = new[] { Barrera("La Línda", "La Fráncia") };

        Assert.True(ReglasBarrerasGeograficas.HayBarreraEntre(barreras, "la linda", "LA FRANCIA"));
    }

    [Fact]
    public void HayBarreraEntre_DevuelveFalso_CuandoNoHayCoincidencia()
    {
        var barreras = new[] { Barrera("La Linda", "La Francia") };

        Assert.False(ReglasBarrerasGeograficas.HayBarreraEntre(barreras, "Sacatín", "Villa Pilar"));
    }

    [Fact]
    public void BuscarParConBarrera_DevuelveElPrimerParEnConflicto()
    {
        var barreras = new[] { Barrera("La Linda", "La Francia") };
        var barrios = new List<string> { "Sacatín", "La Linda", "La Francia" };

        var resultado = ReglasBarrerasGeograficas.BuscarParConBarrera(barrios, barreras);

        Assert.NotNull(resultado);
        Assert.Equal("La Linda", resultado!.Value.BarrioA);
        Assert.Equal("La Francia", resultado.Value.BarrioB);
    }

    [Fact]
    public void BuscarParConBarrera_DevuelveNulo_CuandoNoHayConflictos()
    {
        var barreras = new[] { Barrera("La Linda", "La Francia") };
        var barrios = new List<string> { "Sacatín", "Villa Pilar" };

        var resultado = ReglasBarrerasGeograficas.BuscarParConBarrera(barrios, barreras);

        Assert.Null(resultado);
    }
}
