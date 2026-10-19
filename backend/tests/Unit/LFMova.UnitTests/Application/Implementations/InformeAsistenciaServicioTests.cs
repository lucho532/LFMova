using LFMova.Application.DTOs.Soportes;
using LFMova.Application.Implementations;
using LFMova.Application.Implementations.Servicios;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Pruebas del informe de asistencia de una ruta: qué pasajeros incluye cada filtro, el orden y la
/// numeración, la conversión a hora de Colombia, la espera y el aislamiento por empresa.
/// </summary>
public class InformeAsistenciaServicioTests
{
    private const int EmpresaId = 1;
    private const int ServicioId = 1;

    private class GeneradorCapturador : IGeneradorInformeAsistencia
    {
        public InformeAsistenciaRuta? Informe { get; private set; }

        public byte[] Generar(InformeAsistenciaRuta informe)
        {
            Informe = informe;
            return new byte[] { 1 };
        }
    }

    private class ServicioRepositorioFalso(Servicio servicio) : IServicioRepositorio
    {
        public Task<Servicio?> ObtenerPorIdAsync(int servicioId) => Task.FromResult<Servicio?>(servicioId == servicio.ServicioId ? servicio : null);
        public Task<List<Servicio>> ObtenerPorJornadaAsync(int jornadaId) => Task.FromResult(new List<Servicio>());
        public Task<List<Servicio>> ObtenerPorUnidadOperativaAsync(int unidadOperativaId) => Task.FromResult(new List<Servicio>());
        public Task<List<Servicio>> ObtenerPublicadosPorTipoAsync(TipoServicio tipo) => Task.FromResult(new List<Servicio>());
        public Task<List<Servicio>> ObtenerPendientesDeProgramacionAsync(int empresaId, DateOnly desde) => Task.FromResult(new List<Servicio>());
        public Task<List<Servicio>> ObtenerActivosPorFechaYHoraAsync(DateOnly fecha, TimeOnly hora) => Task.FromResult(new List<Servicio>());
        public Task AgregarAsync(Servicio servicio) => Task.CompletedTask;
        public Task EliminarAsync(Servicio servicio) => Task.CompletedTask;
        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class ServicioPasajeroRepositorioFalso(params ServicioPasajero[] pasajeros) : IServicioPasajeroRepositorio
    {
        public Task<ServicioPasajero?> ObtenerPorIdAsync(int servicioPasajeroId) => Task.FromResult(pasajeros.FirstOrDefault(p => p.ServicioPasajeroId == servicioPasajeroId));
        public Task<ServicioPasajero?> ObtenerPorProgramacionAsync(int programacionTransporteId) => Task.FromResult<ServicioPasajero?>(null);
        public Task<List<ServicioPasajero>> ObtenerPorServicioAsync(int servicioId) => Task.FromResult(pasajeros.Where(p => p.ServicioId == servicioId).ToList());
        public Task<List<ServicioPasajero>> ObtenerPorEmpleadoAsync(int empleadoId) => Task.FromResult(pasajeros.Where(p => p.EmpleadoId == empleadoId).ToList());
        public Task AgregarAsync(ServicioPasajero servicioPasajero) => Task.CompletedTask;
        public void Descartar(ServicioPasajero servicioPasajero) { }
        public Task EliminarAsync(ServicioPasajero servicioPasajero) => Task.CompletedTask;
        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class EmpleadoRepositorioFalso : IEmpleadoRepositorio
    {
        public Task<Empleado?> ObtenerPorIdAsync(int empleadoId)
            => Task.FromResult<Empleado?>(new Empleado { EmpleadoId = empleadoId, NombreCompleto = $"Empleado {empleadoId}", Usuario = new Usuario { Cedula = $"CC{empleadoId}" } });
        public Task<Empleado?> ObtenerPorUsuarioIdAsync(int usuarioId) => Task.FromResult<Empleado?>(null);
        public Task AgregarAsync(Empleado empleado) => Task.CompletedTask;
        public void Descartar(Empleado empleado) { }
        public Task<List<Empleado>> ObtenerPorEmpresaAsync(int empresaId) => Task.FromResult(new List<Empleado>());
        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class UnidadOperativaRepositorioFalso : IUnidadOperativaRepositorio
    {
        public Task<UnidadOperativa?> ObtenerPorIdAsync(int unidadOperativaId) => Task.FromResult<UnidadOperativa?>(new UnidadOperativa { UnidadOperativaId = unidadOperativaId, ConductorId = 7 });
        public Task<UnidadOperativa?> ObtenerPorVehiculoAsync(int vehiculoId) => Task.FromResult<UnidadOperativa?>(null);
        public Task<List<UnidadOperativa>> ObtenerPorConductorAsync(int conductorId) => Task.FromResult(new List<UnidadOperativa>());
        public Task AgregarAsync(UnidadOperativa unidadOperativa) => Task.CompletedTask;
        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class ConductorRepositorioFalso : IConductorRepositorio
    {
        public Task<Conductor?> ObtenerPorIdAsync(int conductorId) => Task.FromResult<Conductor?>(new Conductor { ConductorId = conductorId, NombreCompleto = "Andre Otalvaro" });
        public Task<Conductor?> ObtenerPorUsuarioIdAsync(int usuarioId) => Task.FromResult<Conductor?>(null);
        public Task<List<Conductor>> ObtenerPorEmpresaAsync(int empresaId) => Task.FromResult(new List<Conductor>());
        public Task AgregarAsync(Conductor conductor) => Task.CompletedTask;
        public Task AgregarVinculacionAsync(VinculacionConductorEmpresa vinculacion) => Task.CompletedTask;
        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class EmpresaRepositorioFalso : IEmpresaRepositorio
    {
        public Task<Empresa?> ObtenerPorIdAsync(int empresaId) => Task.FromResult<Empresa?>(new Empresa { EmpresaId = empresaId, Nombre = "Panamericana" });
        public Task<Empresa?> ObtenerPorCifAsync(string cif) => Task.FromResult<Empresa?>(null);
        public Task<List<Empresa>> ObtenerTodasAsync() => Task.FromResult(new List<Empresa>());
        public Task AgregarAsync(Empresa empresa) => Task.CompletedTask;
        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private static ServicioPasajero Pasajero(int id, EstadoServicioPasajero estado, int orden, DateTime? llegada = null, DateTime? procesado = null) => new()
    {
        ServicioPasajeroId = id, ServicioId = ServicioId, EmpleadoId = id, Estado = estado, Orden = orden,
        HoraLlegadaConductor = llegada, HoraProcesado = procesado
    };

    /// <summary>Ruta con cuatro pasajeros desordenados: B se atendió primero, luego A; C avisó que no asistiría y D sigue programado.</summary>
    private static (InformeAsistenciaServicio servicio, GeneradorCapturador generador) Crear()
    {
        var jornada = new Jornada { JornadaId = 1, EmpresaId = EmpresaId };
        var ruta = new Servicio
        {
            ServicioId = ServicioId, JornadaId = 1, Jornada = jornada, UnidadOperativaId = 1, Tipo = TipoServicio.ENTRADA,
            Fecha = new DateOnly(2026, 10, 15), HoraProgramada = new TimeOnly(6, 0), Sede = new Sede { Nombre = "Panamericana" }
        };
        var pasajeros = new ServicioPasajeroRepositorioFalso(
            Pasajero(1, EstadoServicioPasajero.RECOGIDO, orden: 1, llegada: new DateTime(2026, 10, 15, 11, 0, 0), procesado: new DateTime(2026, 10, 15, 11, 2, 44)),
            Pasajero(2, EstadoServicioPasajero.NO_RECOGIDO, orden: 2, llegada: new DateTime(2026, 10, 15, 10, 58, 0), procesado: new DateTime(2026, 10, 15, 10, 59, 30)),
            Pasajero(3, EstadoServicioPasajero.NO_ASISTIRA, orden: 3),
            Pasajero(4, EstadoServicioPasajero.PROGRAMADO, orden: 4));
        var generador = new GeneradorCapturador();
        var servicio = new InformeAsistenciaServicio(
            new AccesoServicio(new ServicioRepositorioFalso(ruta)), pasajeros, new EmpleadoRepositorioFalso(),
            new UnidadOperativaRepositorioFalso(), new ConductorRepositorioFalso(), new EmpresaRepositorioFalso(), generador);
        return (servicio, generador);
    }

    [Fact]
    public async Task GenerarAsync_Todos_ListaPrimeroLosProcesadosEnOrdenCronologico_YLuegoLosSinProcesar()
    {
        var (servicio, generador) = Crear();

        await servicio.GenerarAsync(EmpresaId, ServicioId, FiltroAsistenciaRuta.TODOS);

        var filas = generador.Informe!.Filas;
        Assert.Equal(new[] { "CC2", "CC1", "CC3", "CC4" }, filas.Select(f => f.Cedula));
        Assert.Equal(new int?[] { 1, 2, null, null }, filas.Select(f => f.Numero));
        Assert.Equal(4, generador.Informe.TotalPasajerosRuta);
    }

    [Fact]
    public async Task GenerarAsync_Asistieron_IncluyeSoloRecogidos_ConservandoSuNumeroEnLaRuta()
    {
        var (servicio, generador) = Crear();

        await servicio.GenerarAsync(EmpresaId, ServicioId, FiltroAsistenciaRuta.ASISTIERON);

        var fila = Assert.Single(generador.Informe!.Filas);
        Assert.Equal("CC1", fila.Cedula);
        Assert.Equal(2, fila.Numero);
        Assert.Equal(4, generador.Informe.TotalPasajerosRuta);
    }

    [Fact]
    public async Task GenerarAsync_NoAsistieron_IncluyeNoRecogidosYQuienesAvisaronQueNoAsistirian()
    {
        var (servicio, generador) = Crear();

        await servicio.GenerarAsync(EmpresaId, ServicioId, FiltroAsistenciaRuta.NO_ASISTIERON);

        var filas = generador.Informe!.Filas;
        Assert.Equal(new[] { EstadoServicioPasajero.NO_RECOGIDO, EstadoServicioPasajero.NO_ASISTIRA }, filas.Select(f => f.Estado));
        Assert.Equal(new int?[] { 1, null }, filas.Select(f => f.Numero));
    }

    [Fact]
    public async Task GenerarAsync_ConvierteLaHoraAHoraDeColombia_YCalculaLaEspera()
    {
        var (servicio, generador) = Crear();

        await servicio.GenerarAsync(EmpresaId, ServicioId, FiltroAsistenciaRuta.ASISTIERON);

        var fila = generador.Informe!.Filas.Single();
        // 11:02:44 UTC es 06:02:44 en Colombia (UTC−5); esperó de 11:00:00 a 11:02:44.
        Assert.Equal(new DateTime(2026, 10, 15, 6, 2, 44), fila.HoraRegistro);
        Assert.Equal(164, fila.EsperaSegundos);
    }

    [Fact]
    public async Task GenerarAsync_SinHorasRegistradas_DejaVaciasLaHoraYLaEspera()
    {
        var (servicio, generador) = Crear();

        await servicio.GenerarAsync(EmpresaId, ServicioId, FiltroAsistenciaRuta.NO_ASISTIERON);

        var avisoDeInasistencia = generador.Informe!.Filas.Single(f => f.Estado == EstadoServicioPasajero.NO_ASISTIRA);
        Assert.Null(avisoDeInasistencia.HoraRegistro);
        Assert.Null(avisoDeInasistencia.EsperaSegundos);
    }

    [Theory]
    [InlineData(FiltroAsistenciaRuta.TODOS, "pasajeros-2026-10-15-0600.xlsx")]
    [InlineData(FiltroAsistenciaRuta.ASISTIERON, "asistieron-2026-10-15-0600.xlsx")]
    [InlineData(FiltroAsistenciaRuta.NO_ASISTIERON, "no-asistieron-2026-10-15-0600.xlsx")]
    public async Task GenerarAsync_NombraElArchivoSegunElFiltro(FiltroAsistenciaRuta filtro, string esperado)
    {
        var (servicio, _) = Crear();

        var archivo = await servicio.GenerarAsync(EmpresaId, ServicioId, filtro);

        Assert.Equal(esperado, archivo.NombreArchivo);
    }

    [Fact]
    public async Task GenerarAsync_IncluyeLosDatosDeLaRuta()
    {
        var (servicio, generador) = Crear();

        await servicio.GenerarAsync(EmpresaId, ServicioId, FiltroAsistenciaRuta.TODOS);

        var informe = generador.Informe!;
        Assert.Equal("Panamericana", informe.NombreEmpresa);
        Assert.Equal("Andre Otalvaro", informe.NombreConductor);
        Assert.Equal(TipoServicio.ENTRADA, informe.Tipo);
    }

    [Fact]
    public async Task GenerarAsync_LanzaExcepcion_CuandoLaRutaEsDeOtraEmpresa()
    {
        var (servicio, generador) = Crear();

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.GenerarAsync(empresaId: 2, ServicioId, FiltroAsistenciaRuta.TODOS));
        Assert.Null(generador.Informe);
    }
}
