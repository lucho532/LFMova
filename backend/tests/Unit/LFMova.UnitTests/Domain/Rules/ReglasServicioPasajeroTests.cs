using LFMova.Domain.Entities;
using LFMova.Domain.Rules;

namespace LFMova.UnitTests.Domain.Rules;

public class ReglasServicioPasajeroTests
{
    [Fact]
    public void ProgramacionCoincideConSedeDelServicio_DevuelveVerdadero_CuandoLaSedeEsLaMisma()
    {
        var programacion = new ProgramacionTransporte { SedeId = 5 };
        var servicio = new Servicio { SedeId = 5 };

        Assert.True(ReglasServicioPasajero.ProgramacionCoincideConSedeDelServicio(programacion, servicio));
    }

    [Fact]
    public void ProgramacionCoincideConSedeDelServicio_DevuelveFalso_CuandoLasSedesDifieren()
    {
        var programacion = new ProgramacionTransporte { SedeId = 5 };
        var servicio = new Servicio { SedeId = 6 };

        Assert.False(ReglasServicioPasajero.ProgramacionCoincideConSedeDelServicio(programacion, servicio));
    }
}
