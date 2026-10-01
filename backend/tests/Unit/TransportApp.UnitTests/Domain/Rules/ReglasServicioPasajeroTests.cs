using TransportApp.Domain.Entities;
using TransportApp.Domain.Rules;

namespace TransportApp.UnitTests.Domain.Rules;

public class ReglasServicioPasajeroTests
{
    [Fact]
    public void PerteneceAlServicio_DevuelveVerdadero_CuandoServicioIdCoincide()
    {
        var servicio = new Servicio { ServicioId = 1 };
        var servicioPasajero = new ServicioPasajero { ServicioId = 1 };

        Assert.True(ReglasServicioPasajero.PerteneceAlServicio(servicioPasajero, servicio));
    }

    [Fact]
    public void EmpleadoCorrespondeAProgramacion_DevuelveFalso_CuandoEmpleadosDifieren()
    {
        var programacion = new ProgramacionTransporte { EmpleadoId = 1 };
        var servicioPasajero = new ServicioPasajero { EmpleadoId = 2 };

        Assert.False(ReglasServicioPasajero.EmpleadoCorrespondeAProgramacion(servicioPasajero, programacion));
    }

    [Fact]
    public void ProgramacionYaAsignada_DevuelveVerdadero_CuandoYaExisteUnServicioPasajeroParaEsaProgramacion()
    {
        var existentes = new[]
        {
            new ServicioPasajero { ProgramacionTransporteId = 7 }
        };

        Assert.True(ReglasServicioPasajero.ProgramacionYaAsignada(7, existentes));
    }

    [Fact]
    public void ProgramacionYaAsignada_DevuelveFalso_CuandoNoExisteNinguno()
    {
        var existentes = Array.Empty<ServicioPasajero>();

        Assert.False(ReglasServicioPasajero.ProgramacionYaAsignada(7, existentes));
    }

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
