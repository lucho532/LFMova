using LFMova.Application.DTOs.Incidencias;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

public class IncidenciaServicioTests
{
    private class ServicioPasajeroRepositorioFalso : IServicioPasajeroRepositorio
    {
        private readonly Dictionary<int, ServicioPasajero> _pasajeros;

        public ServicioPasajeroRepositorioFalso(params ServicioPasajero[] pasajeros)
            => _pasajeros = pasajeros.ToDictionary(p => p.ServicioPasajeroId);

        public Task<ServicioPasajero?> ObtenerPorIdAsync(int servicioPasajeroId)
            => Task.FromResult(_pasajeros.TryGetValue(servicioPasajeroId, out var p) ? p : null);

        public Task<ServicioPasajero?> ObtenerPorProgramacionAsync(int programacionTransporteId) => Task.FromResult<ServicioPasajero?>(null);

        public Task<List<ServicioPasajero>> ObtenerPorServicioAsync(int servicioId)
            => Task.FromResult(_pasajeros.Values.Where(p => p.ServicioId == servicioId).ToList());

        public Task<List<ServicioPasajero>> ObtenerPorEmpleadoAsync(int empleadoId)
            => Task.FromResult(_pasajeros.Values.Where(p => p.EmpleadoId == empleadoId).ToList());

        public Task AgregarAsync(ServicioPasajero servicioPasajero) => Task.CompletedTask;

        public void Descartar(ServicioPasajero servicioPasajero) { }

        public Task EliminarAsync(ServicioPasajero servicioPasajero) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class ServicioRepositorioFalso : IServicioRepositorio
    {
        private readonly Dictionary<int, Servicio> _servicios;

        public ServicioRepositorioFalso(params Servicio[] servicios)
            => _servicios = servicios.ToDictionary(s => s.ServicioId);

        public Task<Servicio?> ObtenerPorIdAsync(int servicioId)
            => Task.FromResult(_servicios.TryGetValue(servicioId, out var s) ? s : null);

        public Task<List<Servicio>> ObtenerPorJornadaAsync(int jornadaId) => Task.FromResult(new List<Servicio>());

        public Task<List<Servicio>> ObtenerPorUnidadOperativaAsync(int unidadOperativaId) => Task.FromResult(new List<Servicio>());

        public Task<List<Servicio>> ObtenerPublicadosPorTipoAsync(TipoServicio tipo) => Task.FromResult(new List<Servicio>());


        public Task<List<Servicio>> ObtenerPendientesDeProgramacionAsync(int empresaId, DateOnly desde) => Task.FromResult(new List<Servicio>());



        public Task<List<Servicio>> ObtenerActivosPorFechaYHoraAsync(DateOnly fecha, TimeOnly hora) => Task.FromResult(new List<Servicio>());

        public Task AgregarAsync(Servicio servicio) => Task.CompletedTask;

        public Task EliminarAsync(Servicio servicio) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class UnidadOperativaRepositorioFalso : IUnidadOperativaRepositorio
    {
        private readonly Dictionary<int, UnidadOperativa> _unidades;

        public UnidadOperativaRepositorioFalso(params UnidadOperativa[] unidades)
            => _unidades = unidades.ToDictionary(u => u.UnidadOperativaId);

        public Task<UnidadOperativa?> ObtenerPorIdAsync(int unidadOperativaId)
            => Task.FromResult(_unidades.TryGetValue(unidadOperativaId, out var u) ? u : null);

        public Task<UnidadOperativa?> ObtenerPorVehiculoAsync(int vehiculoId) => Task.FromResult<UnidadOperativa?>(null);

        public Task<List<UnidadOperativa>> ObtenerPorConductorAsync(int conductorId) => Task.FromResult(new List<UnidadOperativa>());

        public Task AgregarAsync(UnidadOperativa unidadOperativa) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class ConductorRepositorioFalso : IConductorRepositorio
    {
        private readonly Dictionary<int, Conductor> _conductores;

        public ConductorRepositorioFalso(params Conductor[] conductores)
            => _conductores = conductores.ToDictionary(c => c.ConductorId);

        public Task<Conductor?> ObtenerPorIdAsync(int conductorId)
            => Task.FromResult(_conductores.TryGetValue(conductorId, out var c) ? c : null);

        public Task<Conductor?> ObtenerPorUsuarioIdAsync(int usuarioId)
            => Task.FromResult(_conductores.Values.FirstOrDefault(c => c.UsuarioId == usuarioId));

        public Task<List<Conductor>> ObtenerPorEmpresaAsync(int empresaId) => Task.FromResult(new List<Conductor>());

        public Task AgregarAsync(Conductor conductor) => Task.CompletedTask;

        public Task AgregarVinculacionAsync(VinculacionConductorEmpresa vinculacion) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class IncidenciaRepositorioFalso : IIncidenciaRepositorio
    {
        public readonly List<Incidencia> Incidencias = new();
        private int _siguienteId = 1;

        public Task<Incidencia?> ObtenerPorIdAsync(int incidenciaId)
            => Task.FromResult(Incidencias.FirstOrDefault(i => i.IncidenciaId == incidenciaId));

        public Task<List<Incidencia>> ObtenerPorServicioPasajeroAsync(int servicioPasajeroId)
            => Task.FromResult(Incidencias.Where(i => i.ServicioPasajeroId == servicioPasajeroId).ToList());

        public List<ServicioPasajero> PasajerosPorServicio = new();

        public Task<List<Incidencia>> ObtenerPorServicioIdAsync(int servicioId)
        {
            var idsDePasajerosDelServicio = PasajerosPorServicio.Where(p => p.ServicioId == servicioId).Select(p => p.ServicioPasajeroId).ToHashSet();
            return Task.FromResult(Incidencias.Where(i => idsDePasajerosDelServicio.Contains(i.ServicioPasajeroId)).ToList());
        }

        public Task AgregarAsync(Incidencia incidencia)
        {
            incidencia.IncidenciaId = _siguienteId++;
            Incidencias.Add(incidencia);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class EvidenciaRepositorioFalso : IEvidenciaRepositorio
    {
        public readonly List<Evidencia> Evidencias = new();
        private int _siguienteId = 1;

        public Task<List<Evidencia>> ObtenerPorIncidenciaAsync(int incidenciaId)
            => Task.FromResult(Evidencias.Where(e => e.IncidenciaId == incidenciaId).ToList());

        public Task AgregarAsync(Evidencia evidencia)
        {
            evidencia.EvidenciaId = _siguienteId++;
            Evidencias.Add(evidencia);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private const int EmpresaId = 1;
    private const int UsuarioIdConductor = 200;

    private ServicioPasajero _pasajeroCreado = new();

    private (IncidenciaServicio servicio, IncidenciaRepositorioFalso incidencias, EvidenciaRepositorioFalso evidencias) CrearServicio(
        bool conUnidadAsignada = true, EstadoServicio estado = EstadoServicio.EN_CURSO, EstadoServicioPasajero estadoPasajero = EstadoServicioPasajero.CONDUCTOR_LLEGO)
    {
        var jornada = new Jornada { JornadaId = 1, EmpresaId = EmpresaId, FechaOperativa = new DateOnly(2026, 1, 20) };
        var servicioEntidad = new Servicio { ServicioId = 1, JornadaId = 1, UnidadOperativaId = conUnidadAsignada ? 1 : null, Estado = estado, Jornada = jornada };
        var servicioRepo = new ServicioRepositorioFalso(servicioEntidad);
        var pasajero = new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, EmpleadoId = 1, Estado = estadoPasajero };
        _pasajeroCreado = pasajero;
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso(pasajero);
        var unidadRepo = new UnidadOperativaRepositorioFalso(new UnidadOperativa { UnidadOperativaId = 1, ConductorId = 1, VehiculoId = 1, Activa = true });
        var conductorRepo = new ConductorRepositorioFalso(new Conductor { ConductorId = 1, UsuarioId = UsuarioIdConductor, Activo = true });
        var incidenciaRepo = new IncidenciaRepositorioFalso();
        incidenciaRepo.PasajerosPorServicio.Add(pasajero);
        var evidenciaRepo = new EvidenciaRepositorioFalso();

        var servicio = new IncidenciaServicio(incidenciaRepo, evidenciaRepo, pasajeroRepo, servicioRepo, unidadRepo, conductorRepo);

        return (servicio, incidenciaRepo, evidenciaRepo);
    }

    [Fact]
    public async Task ObtenerUsuarioIdConductorAsync_DevuelveElUsuarioIdDelConductorAsignado()
    {
        var (servicio, _, _) = CrearServicio();

        var usuarioId = await servicio.ObtenerUsuarioIdConductorAsync(EmpresaId, 1);

        Assert.Equal(UsuarioIdConductor, usuarioId);
    }

    [Fact]
    public async Task ObtenerUsuarioIdConductorAsync_DevuelveNulo_CuandoElServicioNoTieneUnidadAsignada()
    {
        var (servicio, _, _) = CrearServicio(conUnidadAsignada: false);

        var usuarioId = await servicio.ObtenerUsuarioIdConductorAsync(EmpresaId, 1);

        Assert.Null(usuarioId);
    }

    [Fact]
    public async Task CrearAsync_RegistraLaIncidenciaConSuUbicacion()
    {
        var (servicio, incidencias, _) = CrearServicio();

        var incidencia = await servicio.CrearAsync(EmpresaId, 1, new CrearIncidenciaDto
        {
            Tipo = TipoIncidencia.NO_CONTESTA,
            Descripcion = "El empleado no respondió.",
            Latitud = 4.65,
            Longitud = -74.05
        });

        Assert.Single(incidencias.Incidencias);
        Assert.Equal(TipoIncidencia.NO_CONTESTA, incidencia.Tipo);
        Assert.Equal(4.65, incidencia.Latitud);
        Assert.Equal(-74.05, incidencia.Longitud);
    }

    [Theory]
    [InlineData(TipoIncidencia.NO_CONTESTA)]
    [InlineData(TipoIncidencia.NO_SE_ENCUENTRA)]
    [InlineData(TipoIncidencia.DIRECCION_INCORRECTA)]
    [InlineData(TipoIncidencia.NO_SE_PUDO_RECOGER)]
    [InlineData(TipoIncidencia.UBICACION_MODIFICADA)]
    [InlineData(TipoIncidencia.OTRA)]
    public async Task CrearAsync_DejaAlPasajeroComoNoRecogido_ConCualquierTipoDeIncidencia(TipoIncidencia tipo)
    {
        var (servicio, _, _) = CrearServicio();
        _pasajeroCreado.Estado = EstadoServicioPasajero.CONDUCTOR_LLEGO;

        await servicio.CrearAsync(EmpresaId, 1, new CrearIncidenciaDto { Tipo = tipo, Descripcion = "x" });

        Assert.Equal(EstadoServicioPasajero.NO_RECOGIDO, _pasajeroCreado.Estado);
        Assert.NotNull(_pasajeroCreado.HoraProcesado);
    }

    [Theory]
    [InlineData(EstadoServicioPasajero.PROGRAMADO)]
    [InlineData(EstadoServicioPasajero.CONFIRMADO)]
    [InlineData(EstadoServicioPasajero.RECOGIDO)]
    [InlineData(EstadoServicioPasajero.NO_RECOGIDO)]
    public async Task CrearAsync_LanzaExcepcion_CuandoElConductorNoHaMarcadoLaLlegada(EstadoServicioPasajero estadoPasajero)
    {
        var (servicio, _, _) = CrearServicio(estadoPasajero: estadoPasajero);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(EmpresaId, 1, new CrearIncidenciaDto { Tipo = TipoIncidencia.NO_CONTESTA, Descripcion = "x" }));
    }

    [Theory]
    [InlineData(EstadoServicio.PUBLICADO)]
    [InlineData(EstadoServicio.ASIGNADO)]
    [InlineData(EstadoServicio.FINALIZADO)]
    public async Task CrearAsync_LanzaExcepcion_CuandoElServicioNoEstaEnCurso(EstadoServicio estado)
    {
        var (servicio, _, _) = CrearServicio(estado: estado);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(EmpresaId, 1, new CrearIncidenciaDto { Tipo = TipoIncidencia.OTRA }));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoElPasajeroNoExisteEnLaEmpresa()
    {
        var (servicio, _, _) = CrearServicio();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(empresaId: 2, 1, new CrearIncidenciaDto { Tipo = TipoIncidencia.OTRA }));
    }

    [Fact]
    public async Task ObtenerPorServicioPasajeroAsync_IncluyeLasEvidenciasDeCadaIncidencia()
    {
        var (servicio, _, _) = CrearServicio();
        var incidencia = await servicio.CrearAsync(EmpresaId, 1, new CrearIncidenciaDto { Tipo = TipoIncidencia.NO_SE_ENCUENTRA });
        await servicio.AgregarEvidenciaAsync(EmpresaId, 1, incidencia.IncidenciaId, new AgregarEvidenciaDto { Tipo = TipoEvidencia.FOTOGRAFIA, ReferenciaArchivo = "foto-1.jpg" });

        var incidenciasDelPasajero = await servicio.ObtenerPorServicioPasajeroAsync(EmpresaId, 1);

        Assert.Single(incidenciasDelPasajero);
        Assert.Single(incidenciasDelPasajero[0].Evidencias);
        Assert.Equal("foto-1.jpg", incidenciasDelPasajero[0].Evidencias[0].ReferenciaArchivo);
    }

    [Fact]
    public async Task ObtenerPorServicioAsync_DevuelveLasIncidenciasDeTodosLosPasajerosDelServicio()
    {
        var (servicio, _, _) = CrearServicio();
        var incidencia = await servicio.CrearAsync(EmpresaId, 1, new CrearIncidenciaDto { Tipo = TipoIncidencia.NO_CONTESTA, Descripcion = "x" });
        await servicio.AgregarEvidenciaAsync(EmpresaId, 1, incidencia.IncidenciaId, new AgregarEvidenciaDto { Tipo = TipoEvidencia.FOTOGRAFIA, ReferenciaArchivo = "foto-1.jpg" });

        var incidenciasDelServicio = await servicio.ObtenerPorServicioAsync(EmpresaId, jornadaId: 1, servicioId: 1);

        Assert.Single(incidenciasDelServicio);
        Assert.Equal(1, incidenciasDelServicio[0].ServicioPasajeroId);
        Assert.Single(incidenciasDelServicio[0].Evidencias);
    }

    [Fact]
    public async Task ObtenerPorServicioAsync_LanzaExcepcion_CuandoElServicioNoExisteEnLaEmpresa()
    {
        var (servicio, _, _) = CrearServicio();

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ObtenerPorServicioAsync(empresaId: 2, jornadaId: 1, servicioId: 1));
    }

    [Fact]
    public async Task AgregarEvidenciaAsync_LanzaExcepcion_CuandoLaIncidenciaNoExiste()
    {
        var (servicio, _, _) = CrearServicio();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.AgregarEvidenciaAsync(EmpresaId, 1, 999, new AgregarEvidenciaDto { Tipo = TipoEvidencia.FOTOGRAFIA, ReferenciaArchivo = "foto.jpg" }));
    }

    [Fact]
    public async Task AgregarEvidenciaAsync_LanzaExcepcion_CuandoLaIncidenciaPerteneceAOtroPasajero()
    {
        var (servicio, _, _) = CrearServicio();
        var incidencia = await servicio.CrearAsync(EmpresaId, 1, new CrearIncidenciaDto { Tipo = TipoIncidencia.OTRA });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.AgregarEvidenciaAsync(EmpresaId, servicioPasajeroId: 2, incidencia.IncidenciaId,
                new AgregarEvidenciaDto { Tipo = TipoEvidencia.FOTOGRAFIA, ReferenciaArchivo = "foto.jpg" }));
    }
}
