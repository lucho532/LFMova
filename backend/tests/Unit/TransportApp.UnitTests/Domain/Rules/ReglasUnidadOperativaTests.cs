using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Domain.Rules;

namespace TransportApp.UnitTests.Domain.Rules;

public class ReglasUnidadOperativaTests
{
    [Fact]
    public void EsConsistente_DevuelveVerdadero_CuandoConductorYVehiculoCoinciden()
    {
        var vehiculo = new Vehiculo { VehiculoId = 10, ConductorId = 5 };
        var unidad = new UnidadOperativa { VehiculoId = 10, ConductorId = 5 };

        Assert.True(ReglasUnidadOperativa.EsConsistente(unidad, vehiculo));
    }

    [Fact]
    public void EsConsistente_DevuelveFalso_CuandoElConductorNoCoincideConElDelVehiculo()
    {
        var vehiculo = new Vehiculo { VehiculoId = 10, ConductorId = 5 };
        var unidad = new UnidadOperativa { VehiculoId = 10, ConductorId = 99 };

        Assert.False(ReglasUnidadOperativa.EsConsistente(unidad, vehiculo));
    }

    [Fact]
    public void EsConsistente_DevuelveFalso_CuandoElVehiculoNoEsElIndicado()
    {
        var vehiculo = new Vehiculo { VehiculoId = 10, ConductorId = 5 };
        var unidad = new UnidadOperativa { VehiculoId = 11, ConductorId = 5 };

        Assert.False(ReglasUnidadOperativa.EsConsistente(unidad, vehiculo));
    }

    [Fact]
    public void HayConflictoTemporal_DevuelveVerdadero_CuandoOtroServicioTieneLaMismaFechaYHora()
    {
        var candidato = new Servicio { ServicioId = 1, Fecha = new DateOnly(2026, 1, 20), HoraProgramada = new TimeOnly(7, 0) };
        var otros = new[]
        {
            new Servicio { ServicioId = 2, Fecha = new DateOnly(2026, 1, 20), HoraProgramada = new TimeOnly(7, 0), Estado = EstadoServicio.ASIGNADO }
        };

        Assert.True(ReglasUnidadOperativa.HayConflictoTemporal(candidato, otros));
    }

    [Fact]
    public void HayConflictoTemporal_DevuelveFalso_CuandoSonEntradaYSalidaDeLaMismaSedeALaMismaHora()
    {
        var candidato = new Servicio { ServicioId = 1, SedeId = 3, Tipo = TipoServicio.SALIDA, Fecha = new DateOnly(2026, 1, 20), HoraProgramada = new TimeOnly(1, 0) };
        var otros = new[]
        {
            new Servicio { ServicioId = 2, SedeId = 3, Tipo = TipoServicio.ENTRADA, Fecha = new DateOnly(2026, 1, 20), HoraProgramada = new TimeOnly(1, 0), Estado = EstadoServicio.ASIGNADO }
        };

        Assert.False(ReglasUnidadOperativa.HayConflictoTemporal(candidato, otros));
    }

    [Fact]
    public void HayConflictoTemporal_DevuelveVerdadero_CuandoSonEntradaYSalidaALaMismaHoraPeroDeSedesDistintas()
    {
        var candidato = new Servicio { ServicioId = 1, SedeId = 3, Tipo = TipoServicio.SALIDA, Fecha = new DateOnly(2026, 1, 20), HoraProgramada = new TimeOnly(1, 0) };
        var otros = new[]
        {
            new Servicio { ServicioId = 2, SedeId = 4, Tipo = TipoServicio.ENTRADA, Fecha = new DateOnly(2026, 1, 20), HoraProgramada = new TimeOnly(1, 0), Estado = EstadoServicio.ASIGNADO }
        };

        Assert.True(ReglasUnidadOperativa.HayConflictoTemporal(candidato, otros));
    }

    [Fact]
    public void HayConflictoTemporal_DevuelveFalso_CuandoLaHoraDifiere()
    {
        var candidato = new Servicio { ServicioId = 1, Fecha = new DateOnly(2026, 1, 20), HoraProgramada = new TimeOnly(7, 0) };
        var otros = new[]
        {
            new Servicio { ServicioId = 2, Fecha = new DateOnly(2026, 1, 20), HoraProgramada = new TimeOnly(8, 0), Estado = EstadoServicio.ASIGNADO }
        };

        Assert.False(ReglasUnidadOperativa.HayConflictoTemporal(candidato, otros));
    }

    [Fact]
    public void HayConflictoTemporal_IgnoraServiciosCancelados()
    {
        var candidato = new Servicio { ServicioId = 1, Fecha = new DateOnly(2026, 1, 20), HoraProgramada = new TimeOnly(7, 0) };
        var otros = new[]
        {
            new Servicio { ServicioId = 2, Fecha = new DateOnly(2026, 1, 20), HoraProgramada = new TimeOnly(7, 0), Estado = EstadoServicio.CANCELADO }
        };

        Assert.False(ReglasUnidadOperativa.HayConflictoTemporal(candidato, otros));
    }

    [Fact]
    public void HayConflictoTemporal_IgnoraServiciosFinalizados()
    {
        var candidato = new Servicio { ServicioId = 1, Fecha = new DateOnly(2026, 1, 20), HoraProgramada = new TimeOnly(7, 0) };
        var otros = new[]
        {
            new Servicio { ServicioId = 2, Fecha = new DateOnly(2026, 1, 20), HoraProgramada = new TimeOnly(7, 0), Estado = EstadoServicio.FINALIZADO }
        };

        Assert.False(ReglasUnidadOperativa.HayConflictoTemporal(candidato, otros));
    }

    [Fact]
    public void HayConflictoTemporal_IgnoraAlPropioServicio()
    {
        var candidato = new Servicio { ServicioId = 1, Fecha = new DateOnly(2026, 1, 20), HoraProgramada = new TimeOnly(7, 0) };
        var otros = new[]
        {
            new Servicio { ServicioId = 1, Fecha = new DateOnly(2026, 1, 20), HoraProgramada = new TimeOnly(7, 0), Estado = EstadoServicio.ASIGNADO }
        };

        Assert.False(ReglasUnidadOperativa.HayConflictoTemporal(candidato, otros));
    }
}
