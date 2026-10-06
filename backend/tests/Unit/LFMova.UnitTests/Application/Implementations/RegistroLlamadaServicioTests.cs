using LFMova.Application.DTOs.Llamadas;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.UnitTests.Application.Implementations;

public class RegistroLlamadaServicioTests
{
    private const int EmpresaId = 1;
    private const int ServicioPasajeroId = 10;

    private class RegistroLlamadaRepositorioFalso : IRegistroLlamadaRepositorio
    {
        public List<RegistroLlamada> Registros { get; } = new();

        public Task<RegistroLlamada?> ObtenerPorIdAsync(int registroLlamadaId)
            => Task.FromResult(Registros.FirstOrDefault(r => r.RegistroLlamadaId == registroLlamadaId));

        public Task<List<RegistroLlamada>> ObtenerPorServicioPasajeroAsync(int servicioPasajeroId)
            => Task.FromResult(Registros.Where(r => r.ServicioPasajeroId == servicioPasajeroId).OrderBy(r => r.FechaHora).ToList());

        public Task AgregarAsync(RegistroLlamada registro)
        {
            registro.RegistroLlamadaId = Registros.Count + 1;
            Registros.Add(registro);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class ServicioPasajeroRepositorioFalso : IServicioPasajeroRepositorio
    {
        private readonly List<ServicioPasajero> _pasajeros;

        public ServicioPasajeroRepositorioFalso(params ServicioPasajero[] pasajeros) => _pasajeros = pasajeros.ToList();

        public Task<ServicioPasajero?> ObtenerPorIdAsync(int servicioPasajeroId)
            => Task.FromResult(_pasajeros.FirstOrDefault(p => p.ServicioPasajeroId == servicioPasajeroId));

        public Task<ServicioPasajero?> ObtenerPorProgramacionAsync(int programacionTransporteId) => Task.FromResult<ServicioPasajero?>(null);
        public Task<List<ServicioPasajero>> ObtenerPorServicioAsync(int servicioId) => Task.FromResult(new List<ServicioPasajero>());
        public Task<List<ServicioPasajero>> ObtenerPorEmpleadoAsync(int empleadoId) => Task.FromResult(new List<ServicioPasajero>());
        public Task AgregarAsync(ServicioPasajero servicioPasajero) => Task.CompletedTask;
        public void Descartar(ServicioPasajero servicioPasajero) { }
        public Task EliminarAsync(ServicioPasajero servicioPasajero) => Task.CompletedTask;
        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class ServicioRepositorioFalso : IServicioRepositorio
    {
        private readonly Servicio _servicio;

        public ServicioRepositorioFalso(Servicio servicio) => _servicio = servicio;

        public Task<Servicio?> ObtenerPorIdAsync(int servicioId) => Task.FromResult(servicioId == _servicio.ServicioId ? _servicio : null);
        public Task<List<Servicio>> ObtenerPorJornadaAsync(int jornadaId) => Task.FromResult(new List<Servicio>());
        public Task<List<Servicio>> ObtenerPorUnidadOperativaAsync(int unidadOperativaId) => Task.FromResult(new List<Servicio>());
        public Task<List<Servicio>> ObtenerPublicadosPorTipoAsync(TipoServicio tipo) => Task.FromResult(new List<Servicio>());
        public Task<List<Servicio>> ObtenerPendientesDeProgramacionAsync(int empresaId, DateOnly desde) => Task.FromResult(new List<Servicio>());
        public Task<List<Servicio>> ObtenerActivosPorFechaYHoraAsync(DateOnly fecha, TimeOnly hora) => Task.FromResult(new List<Servicio>());
        public Task AgregarAsync(Servicio servicio) => Task.CompletedTask;
        public Task EliminarAsync(Servicio servicio) => Task.CompletedTask;
        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private static (RegistroLlamadaServicio Servicio, RegistroLlamadaRepositorioFalso Repositorio) Crear()
    {
        var servicio = new Servicio { ServicioId = 5, JornadaId = 3, Jornada = new Jornada { JornadaId = 3, EmpresaId = EmpresaId } };
        var pasajero = new ServicioPasajero { ServicioPasajeroId = ServicioPasajeroId, ServicioId = 5 };
        var repositorio = new RegistroLlamadaRepositorioFalso();
        return (new RegistroLlamadaServicio(repositorio, new ServicioPasajeroRepositorioFalso(pasajero), new ServicioRepositorioFalso(servicio)), repositorio);
    }

    [Fact]
    public async Task RegistrarAsync_GuardaLaLlamadaConLaHoraDelServidorYLaUbicacion()
    {
        var (servicio, repositorio) = Crear();
        var antes = DateTime.UtcNow;

        var llamada = await servicio.RegistrarAsync(EmpresaId, ServicioPasajeroId, new RegistrarLlamadaDto { Latitud = 5.06, Longitud = -75.5 });

        var guardada = Assert.Single(repositorio.Registros);
        Assert.Equal(ServicioPasajeroId, guardada.ServicioPasajeroId);
        Assert.InRange(guardada.FechaHora, antes, DateTime.UtcNow);
        Assert.Equal(5.06, llamada.Latitud);
        Assert.Null(llamada.DuracionAproximadaSegundos);
    }

    [Fact]
    public async Task RegistrarAsync_PasajeroDeOtraEmpresa_Falla()
    {
        var (servicio, repositorio) = Crear();

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.RegistrarAsync(99, ServicioPasajeroId, new RegistrarLlamadaDto()));
        Assert.Empty(repositorio.Registros);
    }

    [Fact]
    public async Task RegistrarDuracionAsync_GuardaLaDuracionUnaSolaVez()
    {
        var (servicio, _) = Crear();
        var llamada = await servicio.RegistrarAsync(EmpresaId, ServicioPasajeroId, new RegistrarLlamadaDto());

        await servicio.RegistrarDuracionAsync(EmpresaId, ServicioPasajeroId, llamada.RegistroLlamadaId, 25);
        var resultado = await servicio.RegistrarDuracionAsync(EmpresaId, ServicioPasajeroId, llamada.RegistroLlamadaId, 400);

        Assert.Equal(25, resultado.DuracionAproximadaSegundos);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(ReglasRegistroLlamada.DuracionMaximaSegundos + 1)]
    public async Task RegistrarDuracionAsync_DuracionNoVerosimil_QuedaSinDuracion(int segundos)
    {
        var (servicio, _) = Crear();
        var llamada = await servicio.RegistrarAsync(EmpresaId, ServicioPasajeroId, new RegistrarLlamadaDto());

        var resultado = await servicio.RegistrarDuracionAsync(EmpresaId, ServicioPasajeroId, llamada.RegistroLlamadaId, segundos);

        Assert.Null(resultado.DuracionAproximadaSegundos);
    }

    [Fact]
    public async Task RegistrarDuracionAsync_LlamadaDeOtroPasajero_Falla()
    {
        var (servicio, repositorio) = Crear();
        repositorio.Registros.Add(new RegistroLlamada { RegistroLlamadaId = 77, ServicioPasajeroId = 999 });

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.RegistrarDuracionAsync(EmpresaId, ServicioPasajeroId, 77, 20));
    }

    [Fact]
    public async Task ObtenerPorServicioPasajeroAsync_DevuelveSoloLasLlamadasDeEsePasajero()
    {
        var (servicio, repositorio) = Crear();
        repositorio.Registros.Add(new RegistroLlamada { RegistroLlamadaId = 50, ServicioPasajeroId = 999 });
        await servicio.RegistrarAsync(EmpresaId, ServicioPasajeroId, new RegistrarLlamadaDto());

        var llamadas = await servicio.ObtenerPorServicioPasajeroAsync(EmpresaId, ServicioPasajeroId);

        Assert.Single(llamadas);
    }
}
