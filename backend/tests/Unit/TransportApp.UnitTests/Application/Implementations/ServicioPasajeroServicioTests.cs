using TransportApp.Application.DTOs.Servicios;
using TransportApp.Application.DTOs.ServiciosPasajero;
using TransportApp.Application.Implementations;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;

namespace TransportApp.UnitTests.Application.Implementations;

public class ServicioPasajeroServicioTests
{
    private class ServicioPasajeroRepositorioFalso : IServicioPasajeroRepositorio
    {
        private readonly Dictionary<int, ServicioPasajero> _pasajeros = new();
        private int _siguienteId = 1;

        public Task<ServicioPasajero?> ObtenerPorIdAsync(int servicioPasajeroId)
            => Task.FromResult(_pasajeros.TryGetValue(servicioPasajeroId, out var p) ? p : null);

        public Task<ServicioPasajero?> ObtenerPorProgramacionAsync(int programacionTransporteId)
            => Task.FromResult(_pasajeros.Values.FirstOrDefault(p => p.ProgramacionTransporteId == programacionTransporteId));

        public Task<List<ServicioPasajero>> ObtenerPorServicioAsync(int servicioId)
            => Task.FromResult(_pasajeros.Values.Where(p => p.ServicioId == servicioId).ToList());

        public Task<List<ServicioPasajero>> ObtenerPorEmpleadoAsync(int empleadoId)
            => Task.FromResult(_pasajeros.Values.Where(p => p.EmpleadoId == empleadoId).ToList());

        public Task AgregarAsync(ServicioPasajero servicioPasajero)
        {
            servicioPasajero.ServicioPasajeroId = _siguienteId++;
            _pasajeros[servicioPasajero.ServicioPasajeroId] = servicioPasajero;
            return Task.CompletedTask;
        }

        public void Descartar(ServicioPasajero servicioPasajero) => _pasajeros.Remove(servicioPasajero.ServicioPasajeroId);

        public Task EliminarAsync(ServicioPasajero servicioPasajero)
        {
            _pasajeros.Remove(servicioPasajero.ServicioPasajeroId);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    /// <summary>Fake de IServicioServicio para probar ReasignarAsync: guarda rutas en memoria y crea nuevas con id incremental.</summary>
    private class ServicioServicioFalso : IServicioServicio
    {
        private readonly Dictionary<int, ServicioDto> _servicios;
        // Para MoverAsync, cuando libera la unidad de una ruta vacía: mismas entidades Servicio que usa
        // ServicioRepositorioFalso en la prueba, para que AsignarUnidadAsync las modifique de verdad.
        private readonly List<Servicio>? _entidadesCompartidas;
        private int _siguienteId;

        public ServicioServicioFalso(params ServicioDto[] servicios) : this(null, servicios)
        {
        }

        public ServicioServicioFalso(List<Servicio>? entidadesCompartidas, params ServicioDto[] servicios)
        {
            _entidadesCompartidas = entidadesCompartidas;
            _servicios = servicios.ToDictionary(s => s.ServicioId);
            _siguienteId = (servicios.Length == 0 ? 0 : servicios.Max(s => s.ServicioId)) + 1;
        }

        public Task<ServicioDto> CrearAsync(int empresaId, int jornadaId, CrearServicioDto datos)
        {
            var creado = new ServicioDto
            {
                ServicioId = _siguienteId++,
                EmpresaId = empresaId,
                JornadaId = jornadaId,
                UnidadOperativaId = datos.UnidadOperativaId,
                SedeId = datos.SedeId,
                Fecha = datos.Fecha,
                HoraProgramada = datos.HoraProgramada,
                Tipo = datos.Tipo,
                Estado = EstadoServicio.BORRADOR
            };
            _servicios[creado.ServicioId] = creado;
            return Task.FromResult(creado);
        }

        public Task<ServicioDto?> ObtenerPorIdAsync(int empresaId, int servicioId)
            => Task.FromResult(_servicios.TryGetValue(servicioId, out var s) ? s : null);

        public Task<List<ServicioDto>> ObtenerPorJornadaAsync(int empresaId, int jornadaId)
            => Task.FromResult(_servicios.Values.Where(s => s.JornadaId == jornadaId).ToList());

        public Task<List<ServicioDto>> ObtenerPendientesDeProgramacionAsync(int empresaId, DateOnly desde) => Task.FromResult(new List<ServicioDto>());

        public Task CambiarEstadoAsync(int empresaId, int servicioId, CambiarEstadoServicioDto datos)
        {
            _servicios[servicioId].Estado = datos.NuevoEstado;
            return Task.CompletedTask;
        }

        public Task AsignarUnidadAsync(int empresaId, int servicioId, AsignarUnidadServicioDto datos)
        {
            var entidad = _entidadesCompartidas?.FirstOrDefault(s => s.ServicioId == servicioId);
            if (entidad is null)
            {
                throw new NotImplementedException();
            }

            entidad.UnidadOperativaId = datos.UnidadOperativaId;
            entidad.Estado = EstadoServicio.ASIGNADO;
            return Task.CompletedTask;
        }

        public Task RetirarUnidadAsync(int empresaId, int servicioId) => throw new NotImplementedException();

        public Task DespublicarAsync(int empresaId, int servicioId) => throw new NotImplementedException();

        public Task EliminarAsync(int empresaId, int servicioId) => throw new NotImplementedException();

        public Task<int?> ObtenerUsuarioIdConductorAsignadoAsync(int empresaId, int servicioId) => throw new NotImplementedException();

        public Task IniciarAsync(int empresaId, int servicioId, IniciarServicioDto? datos = null) => throw new NotImplementedException();

        public Task FinalizarAsync(int empresaId, int servicioId, FinalizarServicioDto? datos = null) => throw new NotImplementedException();
    }

    private class ProgramacionRepositorioFalso : IProgramacionTransporteRepositorio
    {
        private readonly Dictionary<int, ProgramacionTransporte> _programaciones;

        public ProgramacionRepositorioFalso(params ProgramacionTransporte[] programaciones)
            => _programaciones = programaciones.ToDictionary(p => p.ProgramacionTransporteId);

        public Task<ProgramacionTransporte?> ObtenerPorIdAsync(int programacionTransporteId)
            => Task.FromResult(_programaciones.TryGetValue(programacionTransporteId, out var p) ? p : null);

        public Task<List<ProgramacionTransporte>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_programaciones.Values.Where(p => p.EmpresaId == empresaId).ToList());

        public Task<ProgramacionTransporte?> ObtenerPorClaveAsync(int empleadoId, int sedeId, DateOnly fecha, TimeOnly hora, TipoServicio tipo)
            => Task.FromResult(_programaciones.Values.FirstOrDefault(p =>
                p.EmpleadoId == empleadoId && p.SedeId == sedeId && p.Fecha == fecha && p.Hora == hora && p.Tipo == tipo));

        public Task AgregarAsync(ProgramacionTransporte programacion) => Task.CompletedTask;

        public void Descartar(ProgramacionTransporte programacion) { }

        public Task EliminarAsync(ProgramacionTransporte programacion)
        {
            _programaciones.Remove(programacion.ProgramacionTransporteId);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class ServicioRepositorioFalso : IServicioRepositorio
    {
        private readonly Dictionary<int, Servicio> _servicios;

        public ServicioRepositorioFalso(params Servicio[] servicios)
            => _servicios = servicios.ToDictionary(s => s.ServicioId);

        public Task<Servicio?> ObtenerPorIdAsync(int servicioId)
            => Task.FromResult(_servicios.TryGetValue(servicioId, out var s) ? s : null);

        public Task<List<Servicio>> ObtenerPorJornadaAsync(int jornadaId)
            => Task.FromResult(_servicios.Values.Where(s => s.JornadaId == jornadaId).ToList());

        public Task<List<Servicio>> ObtenerPorUnidadOperativaAsync(int unidadOperativaId)
            => Task.FromResult(_servicios.Values.Where(s => s.UnidadOperativaId == unidadOperativaId).ToList());

        public Task<List<Servicio>> ObtenerPublicadosPorTipoAsync(TipoServicio tipo)
            => Task.FromResult(_servicios.Values.Where(s => s.Estado == EstadoServicio.PUBLICADO && s.Tipo == tipo).ToList());


        public Task<List<Servicio>> ObtenerPendientesDeProgramacionAsync(int empresaId, DateOnly desde) => Task.FromResult(new List<Servicio>());



        public Task<List<Servicio>> ObtenerActivosPorFechaYHoraAsync(DateOnly fecha, TimeOnly hora) => Task.FromResult(new List<Servicio>());

        public Task AgregarAsync(Servicio servicio) => Task.CompletedTask;

        public Task EliminarAsync(Servicio servicio)
        {
            _servicios.Remove(servicio.ServicioId);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class EmpleadoRepositorioFalso : IEmpleadoRepositorio
    {
        private readonly Dictionary<int, Empleado> _empleados;

        public EmpleadoRepositorioFalso(params Empleado[] empleados)
            => _empleados = empleados.ToDictionary(e => e.EmpleadoId);

        public Task<Empleado?> ObtenerPorIdAsync(int empleadoId)
            => Task.FromResult(_empleados.TryGetValue(empleadoId, out var e) ? e : null);

        public Task<Empleado?> ObtenerPorUsuarioIdAsync(int usuarioId)
            => Task.FromResult(_empleados.Values.FirstOrDefault(e => e.UsuarioId == usuarioId));

        public Task AgregarAsync(Empleado empleado)
        {
            _empleados[empleado.EmpleadoId] = empleado;
            return Task.CompletedTask;
        }

        public void Descartar(Empleado empleado) { }

        public Task<List<Empleado>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_empleados.Values.Where(e => e.EmpresaId == empresaId).ToList());

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class UbicacionHistoricaRepositorioFalso : IUbicacionRecogidaHistoricaRepositorio
    {
        public readonly List<UbicacionRecogidaHistorica> Ubicaciones = new();

        public Task AgregarAsync(UbicacionRecogidaHistorica ubicacion)
        {
            Ubicaciones.Add(ubicacion);
            return Task.CompletedTask;
        }

        public Task<List<UbicacionRecogidaHistorica>> ObtenerPorEmpleadoAsync(int empleadoId)
            => Task.FromResult(Ubicaciones.Where(u => u.EmpleadoId == empleadoId).OrderByDescending(u => u.FechaRegistro).ToList());

        public Task EliminarPorEmpleadoAsync(int empleadoId)
        {
            Ubicaciones.RemoveAll(u => u.EmpleadoId == empleadoId);
            return Task.CompletedTask;
        }

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

    /// <summary>Fake de IZonaRepositorio para probar AprenderCorredorAsync (el efecto de MoverAsync sobre zonas/corredores).</summary>
    private class ZonaRepositorioFalso : IZonaRepositorio
    {
        private readonly List<Zona> _zonas;

        public ZonaRepositorioFalso(params Zona[] zonas) => _zonas = zonas.ToList();

        public Task<Zona?> ObtenerPorIdAsync(int zonaId) => Task.FromResult(_zonas.FirstOrDefault(z => z.ZonaId == zonaId));

        public Task<List<Zona>> ObtenerPorEmpresaAsync(int empresaId) => Task.FromResult(_zonas.Where(z => z.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(Zona zona)
        {
            _zonas.Add(zona);
            return Task.CompletedTask;
        }

        public void Eliminar(Zona zona) => _zonas.Remove(zona);

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class CorredorVialRepositorioFalso : ICorredorVialRepositorio
    {
        private readonly List<CorredorVial> _corredores = new();
        private int _siguienteId = 1;

        public Task<CorredorVial?> ObtenerPorIdAsync(int corredorVialId) => Task.FromResult(_corredores.FirstOrDefault(c => c.CorredorVialId == corredorVialId));

        public Task<List<CorredorVial>> ObtenerPorEmpresaAsync(int empresaId) => Task.FromResult(_corredores.Where(c => c.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(CorredorVial corredorVial)
        {
            // Si la prueba ya le puso un id fijo (para referenciarlo luego), se respeta; si no, se autoasigna,
            // igual que en la base real donde el id lo genera la base de datos al guardar.
            if (corredorVial.CorredorVialId == 0)
            {
                corredorVial.CorredorVialId = _siguienteId;
            }

            _siguienteId = Math.Max(_siguienteId, corredorVial.CorredorVialId + 1);
            _corredores.Add(corredorVial);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class BarreraGeograficaRepositorioFalso : IBarreraGeograficaRepositorio
    {
        private readonly List<BarreraGeografica> _barreras;

        public BarreraGeograficaRepositorioFalso(params BarreraGeografica[] barreras) => _barreras = barreras.ToList();

        public Task<BarreraGeografica?> ObtenerPorIdAsync(int barreraGeograficaId) => Task.FromResult(_barreras.FirstOrDefault(b => b.BarreraGeograficaId == barreraGeograficaId));

        public Task<List<BarreraGeografica>> ObtenerPorEmpresaAsync(int empresaId) => Task.FromResult(_barreras.Where(b => b.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(BarreraGeografica barrera)
        {
            _barreras.Add(barrera);
            return Task.CompletedTask;
        }

        public void Eliminar(BarreraGeografica barrera) => _barreras.Remove(barrera);

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class NotificacionRepositorioFalso : INotificacionRepositorio
    {
        public readonly List<Notificacion> Notificaciones = new();

        public Task<List<Notificacion>> ObtenerPorUsuarioAsync(int usuarioId)
            => Task.FromResult(Notificaciones.Where(n => n.UsuarioId == usuarioId).ToList());

        public Task<Notificacion?> ObtenerPorIdAsync(int notificacionId)
            => Task.FromResult(Notificaciones.FirstOrDefault(n => n.NotificacionId == notificacionId));

        public Task AgregarAsync(Notificacion notificacion)
        {
            Notificaciones.Add(notificacion);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private const int EmpresaId = 1;

    private static Jornada CrearJornada() => new() { JornadaId = 1, EmpresaId = EmpresaId, FechaOperativa = new DateOnly(2026, 1, 20) };

    private static Servicio CrearServicio(Jornada jornada, int sedeId = 1) => new()
    {
        ServicioId = 1,
        JornadaId = jornada.JornadaId,
        UnidadOperativaId = 1,
        SedeId = sedeId,
        Fecha = new DateOnly(2026, 1, 20),
        HoraProgramada = new TimeOnly(7, 0),
        Tipo = TipoServicio.ENTRADA,
        Estado = EstadoServicio.BORRADOR,
        Jornada = jornada
    };

    private (ServicioPasajeroServicio servicio, ServicioPasajeroRepositorioFalso pasajeros, UbicacionHistoricaRepositorioFalso ubicaciones, NotificacionRepositorioFalso notificaciones, EmpleadoRepositorioFalso empleados)
        CrearServicioConEntidades(ProgramacionTransporte programacion, Servicio servicioEntidad)
    {
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        var programacionRepo = new ProgramacionRepositorioFalso(programacion);
        var servicioRepo = new ServicioRepositorioFalso(servicioEntidad);
        var empleado = new Empleado { EmpleadoId = programacion.EmpleadoId, EmpresaId = EmpresaId, Direccion = "Dirección original", Barrio = "Barrio original", Activo = true };
        var empleadoRepo = new EmpleadoRepositorioFalso(empleado);
        var ubicacionRepo = new UbicacionHistoricaRepositorioFalso();
        var unidadRepo = new UnidadOperativaRepositorioFalso(new UnidadOperativa { UnidadOperativaId = 1, ConductorId = 1, VehiculoId = 1, Activa = true });
        var conductorRepo = new ConductorRepositorioFalso(new Conductor { ConductorId = 1, UsuarioId = 50, Activo = true });
        var notificacionRepo = new NotificacionRepositorioFalso();
        var notificacionServicio = new NotificacionServicio(notificacionRepo);

        var servicio = new ServicioPasajeroServicio(
            pasajeroRepo, programacionRepo, servicioRepo, empleadoRepo, ubicacionRepo, unidadRepo, conductorRepo, notificacionServicio, new ServicioServicioFalso(),
            new ZonaRepositorioFalso(), new CorredorVialRepositorioFalso(), new BarreraGeograficaRepositorioFalso());

        return (servicio, pasajeroRepo, ubicacionRepo, notificacionRepo, empleadoRepo);
    }

    private static ProgramacionTransporte ProgramacionValida(int sedeId = 1) => new()
    {
        ProgramacionTransporteId = 1,
        EmpresaId = EmpresaId,
        EmpleadoId = 1,
        SedeId = sedeId,
        Fecha = new DateOnly(2026, 1, 20),
        Hora = new TimeOnly(7, 0),
        Tipo = TipoServicio.ENTRADA,
        DireccionRecogida = "Calle 1",
        BarrioRecogida = "Centro"
    };

    [Fact]
    public async Task CrearAsync_Crea_CuandoProgramacionYServicioSonConsistentes()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, _, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);

        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });

        Assert.Equal(EstadoServicioPasajero.PROGRAMADO, pasajero.Estado);
        Assert.Equal(1, pasajero.Orden);
        Assert.Equal("Calle 1", pasajero.DireccionRecogida);
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoLaSedeDeLaProgramacionNoCoincideConLaDelServicio()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada, sedeId: 1);
        var (servicio, _, _, _, _) = CrearServicioConEntidades(ProgramacionValida(sedeId: 2), servicioEntidad);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 }));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoLaProgramacionYaFueAsignada()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, _, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);

        await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 }));
    }

    [Fact]
    public async Task ConfirmarAsync_MarcaConfirmado_SinCambiarDireccion_CuandoNoSeProporcionaNuevaDireccion()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, _, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });

        await servicio.ConfirmarAsync(EmpresaId, pasajero.ServicioPasajeroId, new ConfirmarServicioPasajeroDto());

        var pasajeros = await servicio.ObtenerPorServicioAsync(EmpresaId, servicioEntidad.ServicioId);
        Assert.Equal(EstadoServicioPasajero.CONFIRMADO, pasajeros[0].Estado);
        Assert.Equal("Calle 1", pasajeros[0].DireccionRecogida);
    }

    [Fact]
    public async Task ConfirmarAsync_EstableceDireccionHabitual_YConservaLaAnteriorComoHistorica()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, ubicaciones, _, empleados) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });

        await servicio.ConfirmarAsync(EmpresaId, pasajero.ServicioPasajeroId, new ConfirmarServicioPasajeroDto
        {
            NuevaDireccion = "Nueva dirección",
            NuevoBarrio = "Nuevo barrio",
            EstablecerComoHabitual = true
        });

        var empleadoActualizado = await empleados.ObtenerPorIdAsync(1);
        Assert.Equal("Nueva dirección", empleadoActualizado!.Direccion);
        Assert.Single(ubicaciones.Ubicaciones);
        Assert.Equal("Dirección original", ubicaciones.Ubicaciones[0].Direccion);
    }

    [Fact]
    public async Task ConfirmarAsync_NoModificaDireccionHabitual_CuandoNoSeSolicita()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, ubicaciones, _, empleados) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });

        await servicio.ConfirmarAsync(EmpresaId, pasajero.ServicioPasajeroId, new ConfirmarServicioPasajeroDto
        {
            NuevaDireccion = "Solo para este servicio",
            EstablecerComoHabitual = false
        });

        var empleadoActualizado = await empleados.ObtenerPorIdAsync(1);
        Assert.Equal("Dirección original", empleadoActualizado!.Direccion);
        Assert.Empty(ubicaciones.Ubicaciones);
    }

    [Fact]
    public async Task MarcarNoAsistiraAsync_CambiaEstadoYNotificaAlConductor()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, notificaciones, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });

        await servicio.MarcarNoAsistiraAsync(EmpresaId, pasajero.ServicioPasajeroId);

        var pasajeros = await servicio.ObtenerPorServicioAsync(EmpresaId, servicioEntidad.ServicioId);
        Assert.Equal(EstadoServicioPasajero.NO_ASISTIRA, pasajeros[0].Estado);
        Assert.Single(notificaciones.Notificaciones);
        Assert.Equal(50, notificaciones.Notificaciones[0].UsuarioId);
    }

    [Fact]
    public async Task MarcarLlegadaAsync_MarcaConductorLlegoRegistraLaHoraYNotificaAlEmpleado_CuandoElServicioEstaEnCursoYElPasajeroEstaProgramado()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, notificaciones, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });
        servicioEntidad.Estado = EstadoServicio.EN_CURSO;
        var antes = DateTime.UtcNow;

        await servicio.MarcarLlegadaAsync(EmpresaId, pasajero.ServicioPasajeroId);

        var pasajeros = await servicio.ObtenerPorServicioAsync(EmpresaId, servicioEntidad.ServicioId);
        Assert.Equal(EstadoServicioPasajero.CONDUCTOR_LLEGO, pasajeros[0].Estado);
        Assert.NotNull(pasajeros[0].HoraLlegadaConductor);
        Assert.True(pasajeros[0].HoraLlegadaConductor >= antes);
        Assert.Single(notificaciones.Notificaciones);
        Assert.Equal("CONDUCTOR_LLEGO", notificaciones.Notificaciones[0].Tipo);
    }

    [Fact]
    public async Task MarcarLlegadaAsync_LanzaExcepcion_CuandoElServicioNoEstaEnCurso()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, _, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.MarcarLlegadaAsync(EmpresaId, pasajero.ServicioPasajeroId));
    }

    [Fact]
    public async Task MarcarLlegadaAsync_LanzaExcepcion_CuandoElPasajeroYaFueProcesado()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, _, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });
        await servicio.MarcarNoAsistiraAsync(EmpresaId, pasajero.ServicioPasajeroId);
        servicioEntidad.Estado = EstadoServicio.EN_CURSO;

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.MarcarLlegadaAsync(EmpresaId, pasajero.ServicioPasajeroId));
    }

    [Fact]
    public async Task CambiarEstadoAsync_MarcaRecogido_CuandoElConductorYaLlego()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, _, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });
        servicioEntidad.Estado = EstadoServicio.EN_CURSO;
        await servicio.MarcarLlegadaAsync(EmpresaId, pasajero.ServicioPasajeroId);

        await servicio.CambiarEstadoAsync(EmpresaId, pasajero.ServicioPasajeroId, new CambiarEstadoServicioPasajeroDto { NuevoEstado = EstadoServicioPasajero.RECOGIDO });

        var pasajeros = await servicio.ObtenerPorServicioAsync(EmpresaId, servicioEntidad.ServicioId);
        Assert.Equal(EstadoServicioPasajero.RECOGIDO, pasajeros[0].Estado);
        Assert.NotNull(pasajeros[0].HoraProcesado);
    }

    [Fact]
    public async Task CambiarEstadoAsync_GuardaLaUbicacionAutomaticamenteYNotificaAlEmpleado_CuandoEsLaPrimeraVezQueSeLoRecogeConUbicacionConocida()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, ubicaciones, notificaciones, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });
        servicioEntidad.Estado = EstadoServicio.EN_CURSO;
        await servicio.MarcarLlegadaAsync(EmpresaId, pasajero.ServicioPasajeroId);
        await servicio.CompartirUbicacionAsync(EmpresaId, pasajero.ServicioPasajeroId, new CompartirUbicacionDto { Latitud = 4.65, Longitud = -74.05 });

        await servicio.CambiarEstadoAsync(EmpresaId, pasajero.ServicioPasajeroId, new CambiarEstadoServicioPasajeroDto { NuevoEstado = EstadoServicioPasajero.RECOGIDO });

        var guardada = Assert.Single(ubicaciones.Ubicaciones);
        Assert.Equal(4.65, guardada.Latitud);
        Assert.Equal(-74.05, guardada.Longitud);
        Assert.Contains(notificaciones.Notificaciones, n => n.Tipo == "PASAJERO_RECOGIDO");
    }

    [Fact]
    public async Task CambiarEstadoAsync_NoDuplicaLaUbicacionGuardada_CuandoElEmpleadoYaTeniaUna()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, ubicaciones, _, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        await ubicaciones.AgregarAsync(new UbicacionRecogidaHistorica { EmpleadoId = 1, Direccion = "Calle vieja", FechaRegistro = DateTime.UtcNow });
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });
        servicioEntidad.Estado = EstadoServicio.EN_CURSO;
        await servicio.MarcarLlegadaAsync(EmpresaId, pasajero.ServicioPasajeroId);
        await servicio.CompartirUbicacionAsync(EmpresaId, pasajero.ServicioPasajeroId, new CompartirUbicacionDto { Latitud = 4.65, Longitud = -74.05 });

        await servicio.CambiarEstadoAsync(EmpresaId, pasajero.ServicioPasajeroId, new CambiarEstadoServicioPasajeroDto { NuevoEstado = EstadoServicioPasajero.RECOGIDO });

        Assert.Single(ubicaciones.Ubicaciones);
    }

    [Fact]
    public async Task CambiarEstadoAsync_LanzaExcepcion_CuandoLaTransicionNoEsValida()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, _, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CambiarEstadoAsync(EmpresaId, pasajero.ServicioPasajeroId, new CambiarEstadoServicioPasajeroDto { NuevoEstado = EstadoServicioPasajero.RECOGIDO }));
    }

    [Fact]
    public async Task ObtenerPorServicioAsync_IncluyeNombreYTelefonoDelEmpleado()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var programacionRepo = new ProgramacionRepositorioFalso(ProgramacionValida());
        var servicioRepo = new ServicioRepositorioFalso(servicioEntidad);
        var empleado = new Empleado { EmpleadoId = 1, EmpresaId = EmpresaId, NombreCompleto = "Ana Gómez", Telefono = "3001234567", Activo = true };
        var empleadoRepo = new EmpleadoRepositorioFalso(empleado);
        var servicio = new ServicioPasajeroServicio(
            new ServicioPasajeroRepositorioFalso(), programacionRepo, servicioRepo, empleadoRepo,
            new UbicacionHistoricaRepositorioFalso(),
            new UnidadOperativaRepositorioFalso(new UnidadOperativa { UnidadOperativaId = 1, ConductorId = 1, VehiculoId = 1, Activa = true }),
            new ConductorRepositorioFalso(new Conductor { ConductorId = 1, UsuarioId = 50, Activo = true }),
            new NotificacionServicio(new NotificacionRepositorioFalso()), new ServicioServicioFalso(),
            new ZonaRepositorioFalso(), new CorredorVialRepositorioFalso(), new BarreraGeograficaRepositorioFalso());
        await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });

        var pasajeros = await servicio.ObtenerPorServicioAsync(EmpresaId, servicioEntidad.ServicioId);

        Assert.Equal("Ana Gómez", pasajeros[0].NombreCompletoEmpleado);
        Assert.Equal("3001234567", pasajeros[0].TelefonoEmpleado);
    }

    [Fact]
    public async Task ObtenerUsuarioIdEmpleadoAsync_DevuelveElUsuarioIdDelEmpleadoDuenoDelPasajero()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, _, empleados) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });
        var empleado = await empleados.ObtenerPorIdAsync(1);
        empleado!.UsuarioId = 300;

        var usuarioId = await servicio.ObtenerUsuarioIdEmpleadoAsync(EmpresaId, pasajero.ServicioPasajeroId);

        Assert.Equal(300, usuarioId);
    }

    [Fact]
    public async Task CompartirUbicacionAsync_ActualizaLatitudYLongitudYNotificaAlConductor()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, notificaciones, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });

        await servicio.CompartirUbicacionAsync(EmpresaId, pasajero.ServicioPasajeroId, new CompartirUbicacionDto { Latitud = 4.65, Longitud = -74.05 });

        var pasajeros = await servicio.ObtenerPorServicioAsync(EmpresaId, servicioEntidad.ServicioId);
        Assert.Equal(4.65, pasajeros[0].Latitud);
        Assert.Equal(-74.05, pasajeros[0].Longitud);
        // Sin esta notificación, la pantalla del conductor podría no enterarse de la nueva ubicación hasta
        // el siguiente sondeo, y "Navegar" usaría un punto desactualizado mientras tanto.
        Assert.Contains(notificaciones.Notificaciones, n => n.Tipo == "UBICACION_COMPARTIDA");
    }

    [Fact]
    public async Task GuardarUbicacionRecogidaAsync_ActualizaElPasajeroYLaConservaEnElHistorialDelEmpleado()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, _, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });

        await servicio.GuardarUbicacionRecogidaAsync(EmpresaId, pasajero.ServicioPasajeroId, new CompartirUbicacionDto { Latitud = 4.7, Longitud = -74.1 });

        var pasajeros = await servicio.ObtenerPorServicioAsync(EmpresaId, servicioEntidad.ServicioId);
        Assert.Equal(4.7, pasajeros[0].Latitud);
        var anteriores = await servicio.ObtenerUbicacionesAnterioresAsync(EmpresaId, pasajero.ServicioPasajeroId);
        var anterior = Assert.Single(anteriores);
        Assert.Equal(4.7, anterior.Latitud);
        Assert.Equal(-74.1, anterior.Longitud);
    }

    [Fact]
    public async Task EliminarUbicacionGuardadaAsync_BorraElHistorialDelEmpleado()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, _, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });
        await servicio.GuardarUbicacionRecogidaAsync(EmpresaId, pasajero.ServicioPasajeroId, new CompartirUbicacionDto { Latitud = 4.7, Longitud = -74.1 });

        await servicio.EliminarUbicacionGuardadaAsync(EmpresaId, pasajero.ServicioPasajeroId);

        Assert.Empty(await servicio.ObtenerUbicacionesAnterioresAsync(EmpresaId, pasajero.ServicioPasajeroId));
    }

    [Fact]
    public async Task ObtenerUsuarioIdConductorAsync_DevuelveElUsuarioIdDelConductorDeLaUnidadDelPropioServicio()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, _, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });

        var usuarioId = await servicio.ObtenerUsuarioIdConductorAsync(EmpresaId, pasajero.ServicioPasajeroId);

        Assert.Equal(50, usuarioId);
    }

    [Fact]
    public async Task ObtenerUsuarioIdConductorAsync_DevuelveNulo_CuandoElPasajeroNoExiste()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, _, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);

        var usuarioId = await servicio.ObtenerUsuarioIdConductorAsync(EmpresaId, 999);

        Assert.Null(usuarioId);
    }

    [Fact]
    public async Task ObtenerPorUsuarioEmpleadoAsync_DevuelveLosServiciosDelEmpleado()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        servicioEntidad.Estado = EstadoServicio.PUBLICADO;
        var empleado = new Empleado { EmpleadoId = 1, UsuarioId = 300, EmpresaId = EmpresaId, Activo = true };
        var empleadoRepo = new EmpleadoRepositorioFalso(empleado);
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero
        {
            EmpleadoId = 1,
            ServicioId = servicioEntidad.ServicioId,
            ProgramacionTransporteId = 1,
            Estado = EstadoServicioPasajero.PROGRAMADO,
            DireccionRecogida = "Calle 1",
            Servicio = servicioEntidad
        });

        var servicio = new ServicioPasajeroServicio(
            pasajeroRepo, new ProgramacionRepositorioFalso(), new ServicioRepositorioFalso(servicioEntidad), empleadoRepo,
            new UbicacionHistoricaRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ConductorRepositorioFalso(),
            new NotificacionServicio(new NotificacionRepositorioFalso()), new ServicioServicioFalso(),
            new ZonaRepositorioFalso(), new CorredorVialRepositorioFalso(), new BarreraGeograficaRepositorioFalso());

        var resultado = await servicio.ObtenerPorUsuarioEmpleadoAsync(300);

        var dto = Assert.Single(resultado);
        Assert.Equal(EmpresaId, dto.EmpresaId);
        Assert.Equal(servicioEntidad.ServicioId, dto.ServicioId);
        Assert.Equal(EstadoServicioPasajero.PROGRAMADO, dto.EstadoServicioPasajero);
    }

    [Theory]
    [InlineData(EstadoServicio.BORRADOR)]
    [InlineData(EstadoServicio.PENDIENTE_ASIGNACION)]
    [InlineData(EstadoServicio.ASIGNADO)]
    [InlineData(EstadoServicio.CANCELADO)]
    public async Task ObtenerPorUsuarioEmpleadoAsync_NoMuestraServiciosQueElCoordinadorNoHaPublicado(EstadoServicio estado)
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        servicioEntidad.Estado = estado;
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { EmpleadoId = 1, ServicioId = servicioEntidad.ServicioId, ProgramacionTransporteId = 1, Servicio = servicioEntidad });
        var servicio = new ServicioPasajeroServicio(
            pasajeroRepo, new ProgramacionRepositorioFalso(), new ServicioRepositorioFalso(servicioEntidad), new EmpleadoRepositorioFalso(new Empleado { EmpleadoId = 1, UsuarioId = 300, EmpresaId = EmpresaId, Activo = true }),
            new UbicacionHistoricaRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ConductorRepositorioFalso(),
            new NotificacionServicio(new NotificacionRepositorioFalso()), new ServicioServicioFalso(),
            new ZonaRepositorioFalso(), new CorredorVialRepositorioFalso(), new BarreraGeograficaRepositorioFalso());

        Assert.Empty(await servicio.ObtenerPorUsuarioEmpleadoAsync(300));
    }

    [Fact]
    public async Task ObtenerPorUsuarioEmpleadoAsync_LanzaExcepcion_CuandoNoTienePerfilDeEmpleado()
    {
        var servicio = new ServicioPasajeroServicio(
            new ServicioPasajeroRepositorioFalso(), new ProgramacionRepositorioFalso(), new ServicioRepositorioFalso(), new EmpleadoRepositorioFalso(),
            new UbicacionHistoricaRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ConductorRepositorioFalso(),
            new NotificacionServicio(new NotificacionRepositorioFalso()), new ServicioServicioFalso(),
            new ZonaRepositorioFalso(), new CorredorVialRepositorioFalso(), new BarreraGeograficaRepositorioFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ObtenerPorUsuarioEmpleadoAsync(999));
    }

    [Fact]
    public async Task ReordenarAsync_PersisteElNuevoOrden()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        var (servicio, _, _, _, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);
        var pasajero = await servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 });

        await servicio.ReordenarAsync(EmpresaId, pasajero.ServicioPasajeroId, new ReordenarServicioPasajeroDto { NuevoOrden = 5 });

        var pasajeros = await servicio.ObtenerPorServicioAsync(EmpresaId, servicioEntidad.ServicioId);
        Assert.Equal(5, pasajeros[0].Orden);
    }

    private static ServicioPasajeroServicio CrearServicioPasajeroServicio(
        ServicioPasajeroRepositorioFalso pasajeroRepo, Servicio servicioEntidad, ServicioServicioFalso? servicioServicio = null)
        => new(
            pasajeroRepo, new ProgramacionRepositorioFalso(), new ServicioRepositorioFalso(servicioEntidad), new EmpleadoRepositorioFalso(),
            new UbicacionHistoricaRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ConductorRepositorioFalso(),
            new NotificacionServicio(new NotificacionRepositorioFalso()), servicioServicio ?? new ServicioServicioFalso(),
            new ZonaRepositorioFalso(), new CorredorVialRepositorioFalso(), new BarreraGeograficaRepositorioFalso());

    /// <summary>Igual que <see cref="CrearServicioPasajeroServicio"/> pero con varios servicios en el repositorio falso (para MoverAsync, que busca origen y destino por id).</summary>
    private static ServicioPasajeroServicio CrearServicioPasajeroServicioConVariosServicios(
        ServicioPasajeroRepositorioFalso pasajeroRepo, params Servicio[] servicios)
        => new(
            pasajeroRepo, new ProgramacionRepositorioFalso(), new ServicioRepositorioFalso(servicios), new EmpleadoRepositorioFalso(),
            new UbicacionHistoricaRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ConductorRepositorioFalso(),
            new NotificacionServicio(new NotificacionRepositorioFalso()), new ServicioServicioFalso(servicios.ToList()),
            new ZonaRepositorioFalso(), new CorredorVialRepositorioFalso(), new BarreraGeograficaRepositorioFalso());

    [Fact]
    public async Task MoverAsync_MueveAlPasajeroYRespetaElOrden_CuandoMismaSedeHoraYTipo()
    {
        var jornada = CrearJornada();
        var origen = CrearServicio(jornada); // ServicioId = 1
        var destino = new Servicio
        {
            ServicioId = 2, JornadaId = jornada.JornadaId, UnidadOperativaId = 2, SedeId = origen.SedeId,
            Fecha = origen.Fecha, HoraProgramada = origen.HoraProgramada, Tipo = origen.Tipo, Estado = EstadoServicio.ASIGNADO, Jornada = jornada
        };
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = destino.ServicioId, EmpleadoId = 2, ProgramacionTransporteId = 2, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 2", Orden = 1 });
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = origen.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1" });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(origen.ServicioId)).Single();
        var servicio = CrearServicioPasajeroServicioConVariosServicios(pasajeroRepo, origen, destino);

        await servicio.MoverAsync(EmpresaId, pasajero.ServicioPasajeroId, new MoverServicioPasajeroDto { ServicioDestinoId = destino.ServicioId });

        var actualizado = await pasajeroRepo.ObtenerPorIdAsync(pasajero.ServicioPasajeroId);
        Assert.Equal(destino.ServicioId, actualizado!.ServicioId);
        Assert.Equal(2, actualizado.Orden);
    }

    [Fact]
    public async Task MoverAsync_LanzaExcepcion_CuandoElDestinoEsDeOtraSedeUOtraHora()
    {
        var jornada = CrearJornada();
        var origen = CrearServicio(jornada); // ServicioId = 1
        var destino = new Servicio
        {
            ServicioId = 2, JornadaId = jornada.JornadaId, UnidadOperativaId = 2, SedeId = origen.SedeId,
            Fecha = origen.Fecha, HoraProgramada = new TimeOnly(8, 0), Tipo = origen.Tipo, Estado = EstadoServicio.ASIGNADO, Jornada = jornada
        };
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = origen.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1" });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(origen.ServicioId)).Single();
        var servicio = CrearServicioPasajeroServicioConVariosServicios(pasajeroRepo, origen, destino);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.MoverAsync(EmpresaId, pasajero.ServicioPasajeroId, new MoverServicioPasajeroDto { ServicioDestinoId = destino.ServicioId }));
    }

    [Fact]
    public async Task MoverAsync_LanzaExcepcion_CuandoYaEstaEnEseServicio()
    {
        var servicioEntidad = CrearServicio(CrearJornada());
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = servicioEntidad.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1" });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(servicioEntidad.ServicioId)).Single();
        var servicio = CrearServicioPasajeroServicioConVariosServicios(pasajeroRepo, servicioEntidad);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.MoverAsync(EmpresaId, pasajero.ServicioPasajeroId, new MoverServicioPasajeroDto { ServicioDestinoId = servicioEntidad.ServicioId }));
    }

    [Fact]
    public async Task MoverAsync_LanzaExcepcion_CuandoElDestinoEstaCanceladoOFinalizado()
    {
        var jornada = CrearJornada();
        var origen = CrearServicio(jornada); // ServicioId = 1
        var destino = new Servicio
        {
            ServicioId = 2, JornadaId = jornada.JornadaId, UnidadOperativaId = 2, SedeId = origen.SedeId,
            Fecha = origen.Fecha, HoraProgramada = origen.HoraProgramada, Tipo = origen.Tipo, Estado = EstadoServicio.CANCELADO, Jornada = jornada
        };
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = origen.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1" });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(origen.ServicioId)).Single();
        var servicio = CrearServicioPasajeroServicioConVariosServicios(pasajeroRepo, origen, destino);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.MoverAsync(EmpresaId, pasajero.ServicioPasajeroId, new MoverServicioPasajeroDto { ServicioDestinoId = destino.ServicioId }));
    }

    [Fact]
    public async Task MoverAsync_EliminaLaRutaVaciaYAsignaSuUnidadALaRutaPendienteDeConductor()
    {
        var jornada = CrearJornada();
        var origen = CrearServicio(jornada); // ServicioId = 1, UnidadOperativaId = 1
        var destino = new Servicio
        {
            ServicioId = 2, JornadaId = jornada.JornadaId, UnidadOperativaId = 2, SedeId = origen.SedeId,
            Fecha = origen.Fecha, HoraProgramada = origen.HoraProgramada, Tipo = origen.Tipo, Estado = EstadoServicio.ASIGNADO, Jornada = jornada
        };
        // Ruta de otro horario de la misma jornada, todavía sin conductor: debe recibir la unidad que libera "origen".
        var pendiente = new Servicio
        {
            ServicioId = 3, JornadaId = jornada.JornadaId, UnidadOperativaId = null, SedeId = origen.SedeId,
            Fecha = origen.Fecha, HoraProgramada = new TimeOnly(9, 0), Tipo = origen.Tipo, Estado = EstadoServicio.PENDIENTE_ASIGNACION, Jornada = jornada
        };
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = destino.ServicioId, EmpleadoId = 2, ProgramacionTransporteId = 2, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 2" });
        // "origen" solo tiene este pasajero: al moverlo, se queda vacía.
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = origen.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1" });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(origen.ServicioId)).Single();

        var entidades = new List<Servicio> { origen, destino, pendiente };
        var servicioRepo = new ServicioRepositorioFalso(entidades.ToArray());
        var servicio = new ServicioPasajeroServicio(
            pasajeroRepo, new ProgramacionRepositorioFalso(), servicioRepo, new EmpleadoRepositorioFalso(),
            new UbicacionHistoricaRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ConductorRepositorioFalso(),
            new NotificacionServicio(new NotificacionRepositorioFalso()), new ServicioServicioFalso(entidades),
            new ZonaRepositorioFalso(), new CorredorVialRepositorioFalso(), new BarreraGeograficaRepositorioFalso());

        await servicio.MoverAsync(EmpresaId, pasajero.ServicioPasajeroId, new MoverServicioPasajeroDto { ServicioDestinoId = destino.ServicioId });

        Assert.Null(await servicioRepo.ObtenerPorIdAsync(origen.ServicioId));
        var pendienteActualizado = await servicioRepo.ObtenerPorIdAsync(pendiente.ServicioId);
        Assert.Equal(1, pendienteActualizado!.UnidadOperativaId);
        Assert.Equal(EstadoServicio.ASIGNADO, pendienteActualizado.Estado);
    }

    [Fact]
    public async Task MoverAsync_NoAsignaLaUnidadLiberadaAUnaRutaQueChocaConOtraQueYaTiene()
    {
        var jornada = CrearJornada();
        var origen = CrearServicio(jornada); // ServicioId = 1, UnidadOperativaId = 1, hora 07:00
        var destino = new Servicio
        {
            ServicioId = 2, JornadaId = jornada.JornadaId, UnidadOperativaId = 2, SedeId = origen.SedeId,
            Fecha = origen.Fecha, HoraProgramada = origen.HoraProgramada, Tipo = origen.Tipo, Estado = EstadoServicio.ASIGNADO, Jornada = jornada
        };
        // La misma unidad de "origen" ya tiene otra ruta a las 09:00 de la misma jornada.
        var otraRutaDeLaMismaUnidad = new Servicio
        {
            ServicioId = 3, JornadaId = jornada.JornadaId, UnidadOperativaId = 1, SedeId = origen.SedeId,
            Fecha = origen.Fecha, HoraProgramada = new TimeOnly(9, 0), Tipo = origen.Tipo, Estado = EstadoServicio.ASIGNADO, Jornada = jornada
        };
        // Única ruta pendiente de conductor: justo a las 09:00, donde la unidad liberada ya está ocupada.
        var pendienteEnConflicto = new Servicio
        {
            ServicioId = 4, JornadaId = jornada.JornadaId, UnidadOperativaId = null, SedeId = origen.SedeId,
            Fecha = origen.Fecha, HoraProgramada = new TimeOnly(9, 0), Tipo = origen.Tipo, Estado = EstadoServicio.PENDIENTE_ASIGNACION, Jornada = jornada
        };
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = destino.ServicioId, EmpleadoId = 2, ProgramacionTransporteId = 2, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 2" });
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = origen.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1" });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(origen.ServicioId)).Single();

        var entidades = new List<Servicio> { origen, destino, otraRutaDeLaMismaUnidad, pendienteEnConflicto };
        var servicioRepo = new ServicioRepositorioFalso(entidades.ToArray());
        var servicio = new ServicioPasajeroServicio(
            pasajeroRepo, new ProgramacionRepositorioFalso(), servicioRepo, new EmpleadoRepositorioFalso(),
            new UbicacionHistoricaRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ConductorRepositorioFalso(),
            new NotificacionServicio(new NotificacionRepositorioFalso()), new ServicioServicioFalso(entidades),
            new ZonaRepositorioFalso(), new CorredorVialRepositorioFalso(), new BarreraGeograficaRepositorioFalso());

        // No debe lanzar excepción: la ruta pendiente en conflicto simplemente se descarta como candidata.
        await servicio.MoverAsync(EmpresaId, pasajero.ServicioPasajeroId, new MoverServicioPasajeroDto { ServicioDestinoId = destino.ServicioId });

        Assert.Null(await servicioRepo.ObtenerPorIdAsync(origen.ServicioId));
        var pendienteSinTocar = await servicioRepo.ObtenerPorIdAsync(pendienteEnConflicto.ServicioId);
        Assert.Null(pendienteSinTocar!.UnidadOperativaId);
        Assert.Equal(EstadoServicio.PENDIENTE_ASIGNACION, pendienteSinTocar.Estado);
        var otraRutaSinTocar = await servicioRepo.ObtenerPorIdAsync(otraRutaDeLaMismaUnidad.ServicioId);
        Assert.Equal(1, otraRutaSinTocar!.UnidadOperativaId);
    }

    [Fact]
    public async Task MoverAsync_EliminaLaRutaVaciaYDejaElConductorLibre_CuandoNoHayNingunaRutaPendienteDeConductor()
    {
        var jornada = CrearJornada();
        var origen = CrearServicio(jornada); // ServicioId = 1, UnidadOperativaId = 1
        var destino = new Servicio
        {
            ServicioId = 2, JornadaId = jornada.JornadaId, UnidadOperativaId = 2, SedeId = origen.SedeId,
            Fecha = origen.Fecha, HoraProgramada = origen.HoraProgramada, Tipo = origen.Tipo, Estado = EstadoServicio.ASIGNADO, Jornada = jornada
        };
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = destino.ServicioId, EmpleadoId = 2, ProgramacionTransporteId = 2, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 2" });
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = origen.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1" });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(origen.ServicioId)).Single();

        var entidades = new List<Servicio> { origen, destino };
        var servicioRepo = new ServicioRepositorioFalso(entidades.ToArray());
        var servicio = new ServicioPasajeroServicio(
            pasajeroRepo, new ProgramacionRepositorioFalso(), servicioRepo, new EmpleadoRepositorioFalso(),
            new UbicacionHistoricaRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ConductorRepositorioFalso(),
            new NotificacionServicio(new NotificacionRepositorioFalso()), new ServicioServicioFalso(entidades),
            new ZonaRepositorioFalso(), new CorredorVialRepositorioFalso(), new BarreraGeograficaRepositorioFalso());

        await servicio.MoverAsync(EmpresaId, pasajero.ServicioPasajeroId, new MoverServicioPasajeroDto { ServicioDestinoId = destino.ServicioId });

        Assert.Null(await servicioRepo.ObtenerPorIdAsync(origen.ServicioId));
        var restante = Assert.Single(await servicioRepo.ObtenerPorJornadaAsync(jornada.JornadaId));
        Assert.Equal(destino.ServicioId, restante.ServicioId);
        Assert.Equal(2, restante.UnidadOperativaId);
    }

    private async Task<(ServicioPasajeroServicio Servicio, ZonaRepositorioFalso Zonas, CorredorVialRepositorioFalso Corredores, Servicio Origen, Servicio Destino, int ServicioPasajeroIdMovido)>
        PrepararEscenarioDeAprendizajeAsync(Zona zonaMovido, Zona zonaDestino, params BarreraGeografica[] barreras)
    {
        var jornada = CrearJornada();
        var origen = CrearServicio(jornada); // ServicioId = 1, UnidadOperativaId = 1
        var destino = new Servicio
        {
            ServicioId = 2, JornadaId = jornada.JornadaId, UnidadOperativaId = 2, SedeId = origen.SedeId,
            Fecha = origen.Fecha, HoraProgramada = origen.HoraProgramada, Tipo = origen.Tipo, Estado = EstadoServicio.ASIGNADO, Jornada = jornada
        };
        var empleadoMovido = new Empleado { EmpleadoId = 1, EmpresaId = EmpresaId, Barrio = zonaMovido.Barrios[0], Activo = true };
        var empleadoDestino = new Empleado { EmpleadoId = 2, EmpresaId = EmpresaId, Barrio = zonaDestino.Barrios[0], Activo = true };
        var empleadoRepo = new EmpleadoRepositorioFalso(empleadoMovido, empleadoDestino);

        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        // Dos pasajeros más además de los que se mueven, para que "destino" no quede sin gente al revisar cupo.
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = destino.ServicioId, EmpleadoId = 2, ProgramacionTransporteId = 2, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 2" });
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = destino.ServicioId, EmpleadoId = 2, ProgramacionTransporteId = 3, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 3" });
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = origen.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1" });
        var pasajeroMovido = (await pasajeroRepo.ObtenerPorServicioAsync(origen.ServicioId)).Single();

        var entidades = new List<Servicio> { origen, destino };
        var servicioRepo = new ServicioRepositorioFalso(entidades.ToArray());
        var zonasRepo = new ZonaRepositorioFalso(zonaMovido, zonaDestino);
        var corredoresRepo = new CorredorVialRepositorioFalso();
        var barrerasRepo = new BarreraGeograficaRepositorioFalso(barreras);

        var servicio = new ServicioPasajeroServicio(
            pasajeroRepo, new ProgramacionRepositorioFalso(), servicioRepo, empleadoRepo,
            new UbicacionHistoricaRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ConductorRepositorioFalso(),
            new NotificacionServicio(new NotificacionRepositorioFalso()), new ServicioServicioFalso(entidades),
            zonasRepo, corredoresRepo, barrerasRepo);

        return (servicio, zonasRepo, corredoresRepo, origen, destino, pasajeroMovido.ServicioPasajeroId);
    }

    [Fact]
    public async Task MoverAsync_CreaUnCorredorVialNuevo_CuandoNingunaDeLasDosZonasTeniaCorredor()
    {
        var zonaMovido = new Zona { ZonaId = 1, EmpresaId = EmpresaId, Nombre = "Zona A", Barrios = new List<string> { "Barrio A" }, Activa = true };
        var zonaDestino = new Zona { ZonaId = 2, EmpresaId = EmpresaId, Nombre = "Zona B", Barrios = new List<string> { "Barrio B" }, Activa = true };
        var (servicio, zonas, corredores, _, destino, servicioPasajeroId) = await PrepararEscenarioDeAprendizajeAsync(zonaMovido, zonaDestino);

        await servicio.MoverAsync(EmpresaId, servicioPasajeroId, new MoverServicioPasajeroDto { ServicioDestinoId = destino.ServicioId });

        var corredorCreado = Assert.Single(await corredores.ObtenerPorEmpresaAsync(EmpresaId));
        Assert.Equal(corredorCreado.CorredorVialId, zonaMovido.CorredorVialId);
        Assert.Equal(corredorCreado.CorredorVialId, zonaDestino.CorredorVialId);
    }

    [Fact]
    public async Task MoverAsync_AmpliaElCorredorExistente_CuandoUnaDeLasZonasYaTeniaUno()
    {
        var corredorExistente = new CorredorVial { CorredorVialId = 9, EmpresaId = EmpresaId, Nombre = "Corredor existente", Activo = true };
        var zonaMovido = new Zona { ZonaId = 1, EmpresaId = EmpresaId, Nombre = "Zona A", Barrios = new List<string> { "Barrio A" }, Activa = true, CorredorVialId = 9 };
        var zonaDestino = new Zona { ZonaId = 2, EmpresaId = EmpresaId, Nombre = "Zona B", Barrios = new List<string> { "Barrio B" }, Activa = true };
        var (servicio, _, corredores, _, destino, servicioPasajeroId) = await PrepararEscenarioDeAprendizajeAsync(zonaMovido, zonaDestino);
        await corredores.AgregarAsync(corredorExistente);

        await servicio.MoverAsync(EmpresaId, servicioPasajeroId, new MoverServicioPasajeroDto { ServicioDestinoId = destino.ServicioId });

        Assert.Equal(9, zonaDestino.CorredorVialId);
        Assert.Single(await corredores.ObtenerPorEmpresaAsync(EmpresaId)); // no se creó uno nuevo
    }

    [Fact]
    public async Task MoverAsync_NoVinculaLasZonas_CuandoHayUnaBarreraGeograficaEntreSusBarrios()
    {
        var zonaMovido = new Zona { ZonaId = 1, EmpresaId = EmpresaId, Nombre = "Zona A", Barrios = new List<string> { "Barrio A" }, Activa = true };
        var zonaDestino = new Zona { ZonaId = 2, EmpresaId = EmpresaId, Nombre = "Zona B", Barrios = new List<string> { "Barrio B" }, Activa = true };
        var barrera = new BarreraGeografica { BarreraGeograficaId = 1, EmpresaId = EmpresaId, BarrioA = "Barrio A", BarrioB = "Barrio B" };
        var (servicio, _, corredores, _, destino, servicioPasajeroId) = await PrepararEscenarioDeAprendizajeAsync(zonaMovido, zonaDestino, barrera);

        await servicio.MoverAsync(EmpresaId, servicioPasajeroId, new MoverServicioPasajeroDto { ServicioDestinoId = destino.ServicioId });

        Assert.Null(zonaMovido.CorredorVialId);
        Assert.Null(zonaDestino.CorredorVialId);
        Assert.Empty(await corredores.ObtenerPorEmpresaAsync(EmpresaId));
    }

    [Fact]
    public async Task MoverAsync_NoFusionaDosCorredoresYaExistentesYDistintos()
    {
        var zonaMovido = new Zona { ZonaId = 1, EmpresaId = EmpresaId, Nombre = "Zona A", Barrios = new List<string> { "Barrio A" }, Activa = true, CorredorVialId = 9 };
        var zonaDestino = new Zona { ZonaId = 2, EmpresaId = EmpresaId, Nombre = "Zona B", Barrios = new List<string> { "Barrio B" }, Activa = true, CorredorVialId = 10 };
        var (servicio, _, corredores, _, destino, servicioPasajeroId) = await PrepararEscenarioDeAprendizajeAsync(zonaMovido, zonaDestino);
        await corredores.AgregarAsync(new CorredorVial { CorredorVialId = 9, EmpresaId = EmpresaId, Nombre = "Corredor A", Activo = true });
        await corredores.AgregarAsync(new CorredorVial { CorredorVialId = 10, EmpresaId = EmpresaId, Nombre = "Corredor B", Activo = true });

        await servicio.MoverAsync(EmpresaId, servicioPasajeroId, new MoverServicioPasajeroDto { ServicioDestinoId = destino.ServicioId });

        Assert.Equal(9, zonaMovido.CorredorVialId);
        Assert.Equal(10, zonaDestino.CorredorVialId);
    }

    [Fact]
    public async Task MoverAsync_NoAgrandaUnCorredorQueYaAlcanzoElTopeDeZonas()
    {
        // El corredor 9 ya tiene 4 zonas (el tope): una quinta zona, sin relación real con ninguna de
        // ellas más que haber coincidido una vez con un pasajero que sí iba a esa ruta, no debe sumarse.
        var zonaExistente1 = new Zona { ZonaId = 10, EmpresaId = EmpresaId, Nombre = "Existente 1", Barrios = new List<string> { "Barrio E1" }, Activa = true, CorredorVialId = 9 };
        var zonaExistente2 = new Zona { ZonaId = 11, EmpresaId = EmpresaId, Nombre = "Existente 2", Barrios = new List<string> { "Barrio E2" }, Activa = true, CorredorVialId = 9 };
        var zonaExistente3 = new Zona { ZonaId = 12, EmpresaId = EmpresaId, Nombre = "Existente 3", Barrios = new List<string> { "Barrio E3" }, Activa = true, CorredorVialId = 9 };
        var zonaDestino = new Zona { ZonaId = 13, EmpresaId = EmpresaId, Nombre = "Existente 4", Barrios = new List<string> { "Barrio B" }, Activa = true, CorredorVialId = 9 };
        var zonaMovido = new Zona { ZonaId = 1, EmpresaId = EmpresaId, Nombre = "Zona Nueva", Barrios = new List<string> { "Barrio A" }, Activa = true };

        var jornada = CrearJornada();
        var origen = CrearServicio(jornada); // ServicioId = 1, UnidadOperativaId = 1
        var destino = new Servicio
        {
            ServicioId = 2, JornadaId = jornada.JornadaId, UnidadOperativaId = 2, SedeId = origen.SedeId,
            Fecha = origen.Fecha, HoraProgramada = origen.HoraProgramada, Tipo = origen.Tipo, Estado = EstadoServicio.ASIGNADO, Jornada = jornada
        };
        var empleadoMovido = new Empleado { EmpleadoId = 1, EmpresaId = EmpresaId, Barrio = "Barrio A", Activo = true };
        var empleadoDestino = new Empleado { EmpleadoId = 2, EmpresaId = EmpresaId, Barrio = "Barrio B", Activo = true };
        var empleadoRepo = new EmpleadoRepositorioFalso(empleadoMovido, empleadoDestino);

        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = destino.ServicioId, EmpleadoId = 2, ProgramacionTransporteId = 2, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 2" });
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = destino.ServicioId, EmpleadoId = 2, ProgramacionTransporteId = 3, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 3" });
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = origen.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1" });
        var pasajeroMovido = (await pasajeroRepo.ObtenerPorServicioAsync(origen.ServicioId)).Single();

        var entidades = new List<Servicio> { origen, destino };
        var servicioRepo = new ServicioRepositorioFalso(entidades.ToArray());
        var zonasRepo = new ZonaRepositorioFalso(zonaMovido, zonaExistente1, zonaExistente2, zonaExistente3, zonaDestino);

        var servicio = new ServicioPasajeroServicio(
            pasajeroRepo, new ProgramacionRepositorioFalso(), servicioRepo, empleadoRepo,
            new UbicacionHistoricaRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ConductorRepositorioFalso(),
            new NotificacionServicio(new NotificacionRepositorioFalso()), new ServicioServicioFalso(entidades),
            zonasRepo, new CorredorVialRepositorioFalso(), new BarreraGeograficaRepositorioFalso());

        await servicio.MoverAsync(EmpresaId, pasajeroMovido.ServicioPasajeroId, new MoverServicioPasajeroDto { ServicioDestinoId = destino.ServicioId });

        Assert.Null(zonaMovido.CorredorVialId);
    }

    [Fact]
    public async Task CancelarAsync_MarcaCancelado_CuandoElPasajeroNoHaSidoProcesado()
    {
        var servicioEntidad = CrearServicio(CrearJornada());
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero
        {
            ServicioId = servicioEntidad.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.CONFIRMADO, DireccionRecogida = "Calle 1"
        });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(servicioEntidad.ServicioId)).Single();
        var servicio = CrearServicioPasajeroServicio(pasajeroRepo, servicioEntidad);

        await servicio.CancelarAsync(EmpresaId, pasajero.ServicioPasajeroId);

        Assert.Equal(EstadoServicioPasajero.CANCELADO, (await pasajeroRepo.ObtenerPorIdAsync(pasajero.ServicioPasajeroId))!.Estado);
    }

    [Fact]
    public async Task CancelarAsync_LanzaExcepcion_CuandoElPasajeroYaFueProcesado()
    {
        var servicioEntidad = CrearServicio(CrearJornada());
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero
        {
            ServicioId = servicioEntidad.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.RECOGIDO, DireccionRecogida = "Calle 1"
        });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(servicioEntidad.ServicioId)).Single();
        var servicio = CrearServicioPasajeroServicio(pasajeroRepo, servicioEntidad);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CancelarAsync(EmpresaId, pasajero.ServicioPasajeroId));
    }

    [Fact]
    public async Task EliminarAsync_BorraElPasajeroPorCompleto()
    {
        var servicioEntidad = CrearServicio(CrearJornada());
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero
        {
            ServicioId = servicioEntidad.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1"
        });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(servicioEntidad.ServicioId)).Single();
        var servicio = CrearServicioPasajeroServicio(pasajeroRepo, servicioEntidad);

        await servicio.EliminarAsync(EmpresaId, pasajero.ServicioPasajeroId);

        Assert.Null(await pasajeroRepo.ObtenerPorIdAsync(pasajero.ServicioPasajeroId));
    }

    [Fact]
    public async Task EliminarAsync_LanzaExcepcion_CuandoElServicioEstaEnCursoOFinalizado()
    {
        var servicioEntidad = CrearServicio(CrearJornada());
        servicioEntidad.Estado = EstadoServicio.EN_CURSO;
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero
        {
            ServicioId = servicioEntidad.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1"
        });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(servicioEntidad.ServicioId)).Single();
        var servicio = CrearServicioPasajeroServicio(pasajeroRepo, servicioEntidad);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.EliminarAsync(EmpresaId, pasajero.ServicioPasajeroId));
    }

    [Fact]
    public async Task EliminarAsync_TambienEliminaLaRuta_CuandoEraElUltimoPasajero()
    {
        var servicioEntidad = CrearServicio(CrearJornada()); // ServicioId = 1, UnidadOperativaId = 1
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero
        {
            ServicioId = servicioEntidad.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1"
        });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(servicioEntidad.ServicioId)).Single();
        var servicioRepo = new ServicioRepositorioFalso(servicioEntidad);
        var servicio = new ServicioPasajeroServicio(
            pasajeroRepo, new ProgramacionRepositorioFalso(), servicioRepo, new EmpleadoRepositorioFalso(),
            new UbicacionHistoricaRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ConductorRepositorioFalso(),
            new NotificacionServicio(new NotificacionRepositorioFalso()), new ServicioServicioFalso(),
            new ZonaRepositorioFalso(), new CorredorVialRepositorioFalso(), new BarreraGeograficaRepositorioFalso());

        await servicio.EliminarAsync(EmpresaId, pasajero.ServicioPasajeroId);

        Assert.Null(await servicioRepo.ObtenerPorIdAsync(servicioEntidad.ServicioId));
    }

    [Fact]
    public async Task EditarDireccionAsync_ActualizaLaDireccionDeRecogida()
    {
        var servicioEntidad = CrearServicio(CrearJornada());
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero
        {
            ServicioId = servicioEntidad.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1"
        });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(servicioEntidad.ServicioId)).Single();
        var servicio = CrearServicioPasajeroServicio(pasajeroRepo, servicioEntidad);

        await servicio.EditarDireccionAsync(EmpresaId, pasajero.ServicioPasajeroId, new EditarDireccionServicioPasajeroDto { Direccion = "Calle 2 corregida" });

        var actualizado = await pasajeroRepo.ObtenerPorIdAsync(pasajero.ServicioPasajeroId);
        Assert.Equal("Calle 2 corregida", actualizado!.DireccionRecogida);
        Assert.Equal(EstadoServicioPasajero.PROGRAMADO, actualizado.Estado);
    }

    [Fact]
    public async Task EditarDireccionAsync_LanzaExcepcion_CuandoLaDireccionQuedaVacia()
    {
        var servicioEntidad = CrearServicio(CrearJornada());
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero
        {
            ServicioId = servicioEntidad.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1"
        });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(servicioEntidad.ServicioId)).Single();
        var servicio = CrearServicioPasajeroServicio(pasajeroRepo, servicioEntidad);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.EditarDireccionAsync(EmpresaId, pasajero.ServicioPasajeroId, new EditarDireccionServicioPasajeroDto { Direccion = "   " }));
    }

    [Fact]
    public async Task EditarDireccionAsync_LanzaExcepcion_CuandoElServicioEstaEnCursoOFinalizado()
    {
        var servicioEntidad = CrearServicio(CrearJornada());
        servicioEntidad.Estado = EstadoServicio.FINALIZADO;
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero
        {
            ServicioId = servicioEntidad.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1"
        });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(servicioEntidad.ServicioId)).Single();
        var servicio = CrearServicioPasajeroServicio(pasajeroRepo, servicioEntidad);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.EditarDireccionAsync(EmpresaId, pasajero.ServicioPasajeroId, new EditarDireccionServicioPasajeroDto { Direccion = "Calle 2" }));
    }

    [Fact]
    public async Task ReasignarAsync_CreaLaRutaDestino_CuandoNoExisteYMueveAlPasajero()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada); // UnidadOperativaId = 1
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero
        {
            ServicioId = servicioEntidad.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1"
        });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(servicioEntidad.ServicioId)).Single();
        // Arranca el contador de IDs de este repositorio falso lejos del de servicioEntidad (ServicioId 1): en la base real es una
        // sola secuencia y nunca chocarían, pero acá son dos diccionarios en memoria independientes.
        var servicioServicioFalso = new ServicioServicioFalso(new ServicioDto { ServicioId = 500, JornadaId = -1, Estado = EstadoServicio.CANCELADO });
        var servicio = CrearServicioPasajeroServicio(pasajeroRepo, servicioEntidad, servicioServicioFalso);

        await servicio.ReasignarAsync(EmpresaId, pasajero.ServicioPasajeroId, new ReasignarServicioPasajeroDto { UnidadOperativaId = 2 });

        var actualizado = await pasajeroRepo.ObtenerPorIdAsync(pasajero.ServicioPasajeroId);
        Assert.NotEqual(servicioEntidad.ServicioId, actualizado!.ServicioId);
        Assert.Equal(1, actualizado.Orden);

        var creado = (await servicioServicioFalso.ObtenerPorJornadaAsync(EmpresaId, jornada.JornadaId)).Single(s => s.ServicioId == actualizado.ServicioId);
        Assert.Equal(2, creado.UnidadOperativaId);
        Assert.Equal(EstadoServicio.ASIGNADO, creado.Estado);
        Assert.Equal(servicioEntidad.SedeId, creado.SedeId);
        Assert.Equal(servicioEntidad.HoraProgramada, creado.HoraProgramada);
    }

    [Fact]
    public async Task ReasignarAsync_ReutilizaLaRutaDestino_CuandoYaExisteYRespetaElOrden()
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada); // UnidadOperativaId = 1, ServicioId = 1
        var destinoExistente = new ServicioDto
        {
            ServicioId = 99, EmpresaId = EmpresaId, JornadaId = jornada.JornadaId, UnidadOperativaId = 2,
            SedeId = servicioEntidad.SedeId, Fecha = servicioEntidad.Fecha, HoraProgramada = servicioEntidad.HoraProgramada, Tipo = servicioEntidad.Tipo, Estado = EstadoServicio.ASIGNADO
        };
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        // Ya hay alguien en la ruta destino: el reasignado debe quedar después.
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = 99, EmpleadoId = 2, ProgramacionTransporteId = 2, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 2", Orden = 1 });
        await pasajeroRepo.AgregarAsync(new ServicioPasajero { ServicioId = servicioEntidad.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1" });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(servicioEntidad.ServicioId)).Single();
        var servicioServicioFalso = new ServicioServicioFalso(destinoExistente);
        var servicio = CrearServicioPasajeroServicio(pasajeroRepo, servicioEntidad, servicioServicioFalso);

        await servicio.ReasignarAsync(EmpresaId, pasajero.ServicioPasajeroId, new ReasignarServicioPasajeroDto { UnidadOperativaId = 2 });

        var actualizado = await pasajeroRepo.ObtenerPorIdAsync(pasajero.ServicioPasajeroId);
        Assert.Equal(99, actualizado!.ServicioId);
        Assert.Equal(2, actualizado.Orden);
        Assert.Single(await servicioServicioFalso.ObtenerPorJornadaAsync(EmpresaId, jornada.JornadaId));
    }

    [Fact]
    public async Task ReasignarAsync_LanzaExcepcion_CuandoYaEstaEnUnaRutaDeEsaUnidad()
    {
        var servicioEntidad = CrearServicio(CrearJornada()); // UnidadOperativaId = 1
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso();
        await pasajeroRepo.AgregarAsync(new ServicioPasajero
        {
            ServicioId = servicioEntidad.ServicioId, EmpleadoId = 1, ProgramacionTransporteId = 1, Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1"
        });
        var pasajero = (await pasajeroRepo.ObtenerPorServicioAsync(servicioEntidad.ServicioId)).Single();
        var servicio = CrearServicioPasajeroServicio(pasajeroRepo, servicioEntidad);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ReasignarAsync(EmpresaId, pasajero.ServicioPasajeroId, new ReasignarServicioPasajeroDto { UnidadOperativaId = 1 }));
    }
}
