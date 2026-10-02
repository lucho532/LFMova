using LFMova.Application.DTOs.Jornadas;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

public class JornadaServicioTests
{
    private class ServicioRepositorioFalso : IServicioRepositorio
    {
        public readonly List<Servicio> Servicios = new();

        public Task<Servicio?> ObtenerPorIdAsync(int servicioId)
            => Task.FromResult(Servicios.FirstOrDefault(s => s.ServicioId == servicioId));

        public Task<List<Servicio>> ObtenerPorJornadaAsync(int jornadaId)
            => Task.FromResult(Servicios.Where(s => s.JornadaId == jornadaId).ToList());

        public Task<List<Servicio>> ObtenerPorUnidadOperativaAsync(int unidadOperativaId)
            => Task.FromResult(Servicios.Where(s => s.UnidadOperativaId == unidadOperativaId).ToList());

        public Task<List<Servicio>> ObtenerPublicadosPorTipoAsync(TipoServicio tipo)
            => Task.FromResult(Servicios.Where(s => s.Estado == EstadoServicio.PUBLICADO && s.Tipo == tipo).ToList());


        public Task<List<Servicio>> ObtenerPendientesDeProgramacionAsync(int empresaId, DateOnly desde) => Task.FromResult(new List<Servicio>());



        public Task<List<Servicio>> ObtenerActivosPorFechaYHoraAsync(DateOnly fecha, TimeOnly hora) => Task.FromResult(new List<Servicio>());

        public Task AgregarAsync(Servicio servicio)
        {
            Servicios.Add(servicio);
            return Task.CompletedTask;
        }

        public Task EliminarAsync(Servicio servicio)
        {
            Servicios.Remove(servicio);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class JornadaRepositorioFalso : IJornadaRepositorio
    {
        private readonly Dictionary<int, Jornada> _jornadas = new();
        private int _siguienteId = 1;

        public Task<Jornada?> ObtenerPorIdAsync(int jornadaId)
            => Task.FromResult(_jornadas.TryGetValue(jornadaId, out var j) ? j : null);

        public Task<List<Jornada>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_jornadas.Values.Where(j => j.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(Jornada jornada)
        {
            jornada.JornadaId = _siguienteId++;
            _jornadas[jornada.JornadaId] = jornada;
            return Task.CompletedTask;
        }

        public Task EliminarAsync(Jornada jornada)
        {
            _jornadas.Remove(jornada.JornadaId);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class ProgramacionRepositorioFalso : IProgramacionTransporteRepositorio
    {
        public readonly List<ProgramacionTransporte> Programaciones = new();

        public Task<ProgramacionTransporte?> ObtenerPorIdAsync(int programacionTransporteId)
            => Task.FromResult(Programaciones.FirstOrDefault(p => p.ProgramacionTransporteId == programacionTransporteId));

        public Task<List<ProgramacionTransporte>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(Programaciones.Where(p => p.EmpresaId == empresaId).ToList());

        public Task<ProgramacionTransporte?> ObtenerPorClaveAsync(int empleadoId, int sedeId, DateOnly fecha, TimeOnly hora, TipoServicio tipo)
            => Task.FromResult(Programaciones.FirstOrDefault(p =>
                p.EmpleadoId == empleadoId && p.SedeId == sedeId && p.Fecha == fecha && p.Hora == hora && p.Tipo == tipo));

        public Task AgregarAsync(ProgramacionTransporte programacion)
        {
            Programaciones.Add(programacion);
            return Task.CompletedTask;
        }

        public void Descartar(ProgramacionTransporte programacion) => Programaciones.Remove(programacion);

        public Task EliminarAsync(ProgramacionTransporte programacion)
        {
            Programaciones.Remove(programacion);
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

    private class ServicioPasajeroRepositorioFalso : IServicioPasajeroRepositorio
    {
        private readonly List<ServicioPasajero> _pasajeros;

        public ServicioPasajeroRepositorioFalso(params ServicioPasajero[] pasajeros)
            => _pasajeros = pasajeros.ToList();

        public Task<ServicioPasajero?> ObtenerPorIdAsync(int servicioPasajeroId) => Task.FromResult<ServicioPasajero?>(null);

        public Task<ServicioPasajero?> ObtenerPorProgramacionAsync(int programacionTransporteId)
            => Task.FromResult(_pasajeros.FirstOrDefault(p => p.ProgramacionTransporteId == programacionTransporteId));

        public Task<List<ServicioPasajero>> ObtenerPorServicioAsync(int servicioId)
            => Task.FromResult(_pasajeros.Where(p => p.ServicioId == servicioId).ToList());

        public Task<List<ServicioPasajero>> ObtenerPorEmpleadoAsync(int empleadoId)
            => Task.FromResult(_pasajeros.Where(p => p.EmpleadoId == empleadoId).ToList());

        public Task AgregarAsync(ServicioPasajero servicioPasajero) => Task.CompletedTask;

        public void Descartar(ServicioPasajero servicioPasajero) { }

        public Task EliminarAsync(ServicioPasajero servicioPasajero)
        {
            _pasajeros.Remove(servicioPasajero);
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

    private class NotificacionRepositorioFalso : INotificacionRepositorio
    {
        public readonly List<Notificacion> Notificaciones = new();
        private int _siguienteId = 1;

        public Task<List<Notificacion>> ObtenerPorUsuarioAsync(int usuarioId)
            => Task.FromResult(Notificaciones.Where(n => n.UsuarioId == usuarioId).ToList());

        public Task<Notificacion?> ObtenerPorIdAsync(int notificacionId)
            => Task.FromResult(Notificaciones.FirstOrDefault(n => n.NotificacionId == notificacionId));

        public Task AgregarAsync(Notificacion notificacion)
        {
            notificacion.NotificacionId = _siguienteId++;
            Notificaciones.Add(notificacion);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private static JornadaServicio CrearServicio(
        IJornadaRepositorio jornadaRepositorio,
        IServicioRepositorio servicioRepositorio,
        IUnidadOperativaRepositorio? unidadOperativaRepositorio = null,
        IConductorRepositorio? conductorRepositorio = null,
        IServicioPasajeroRepositorio? servicioPasajeroRepositorio = null,
        IEmpleadoRepositorio? empleadoRepositorio = null,
        IProgramacionTransporteRepositorio? programacionRepositorio = null,
        NotificacionRepositorioFalso? notificacionRepositorio = null)
        => JornadaServicioFabrica.Crear(
            jornadaRepositorio,
            servicioRepositorio,
            unidadOperativaRepositorio ?? new UnidadOperativaRepositorioFalso(),
            conductorRepositorio ?? new ConductorRepositorioFalso(),
            servicioPasajeroRepositorio ?? new ServicioPasajeroRepositorioFalso(),
            empleadoRepositorio ?? new EmpleadoRepositorioFalso(),
            programacionRepositorio ?? new ProgramacionRepositorioFalso(),
            new NotificacionServicio(notificacionRepositorio ?? new NotificacionRepositorioFalso()));

    [Fact]
    public async Task CrearAsync_Crea_SinRequerirUnidadOperativa()
    {
        var servicio = CrearServicio(new JornadaRepositorioFalso(), new ServicioRepositorioFalso());

        var jornada = await servicio.CrearAsync(1, new CrearJornadaDto { FechaOperativa = new DateOnly(2026, 1, 20) });

        Assert.Equal(1, jornada.EmpresaId);
        Assert.Equal(new DateOnly(2026, 1, 20), jornada.FechaOperativa);
    }

    [Fact]
    public async Task PublicarAsync_PublicaLosServiciosAsignados()
    {
        var servicioRepo = new ServicioRepositorioFalso();
        var servicio = CrearServicio(new JornadaRepositorioFalso(), servicioRepo);
        var jornada = await servicio.CrearAsync(1, new CrearJornadaDto { FechaOperativa = new DateOnly(2026, 1, 20) });
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 1, JornadaId = jornada.JornadaId, UnidadOperativaId = 1, Estado = EstadoServicio.ASIGNADO });

        await servicio.PublicarAsync(1, jornada.JornadaId);

        Assert.Equal(EstadoServicio.PUBLICADO, servicioRepo.Servicios[0].Estado);
    }

    [Fact]
    public async Task PublicarAsync_LanzaExcepcion_CuandoHayServiciosSinResolver()
    {
        var servicioRepo = new ServicioRepositorioFalso();
        var servicio = CrearServicio(new JornadaRepositorioFalso(), servicioRepo);
        var jornada = await servicio.CrearAsync(1, new CrearJornadaDto { FechaOperativa = new DateOnly(2026, 1, 20) });
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 1, JornadaId = jornada.JornadaId, Estado = EstadoServicio.PENDIENTE_ASIGNACION });

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.PublicarAsync(1, jornada.JornadaId));
    }

    [Fact]
    public async Task PublicarAsync_LanzaExcepcion_CuandoHayServiciosAsignadosSinUnidadOperativa()
    {
        var servicioRepo = new ServicioRepositorioFalso();
        var servicio = CrearServicio(new JornadaRepositorioFalso(), servicioRepo);
        var jornada = await servicio.CrearAsync(1, new CrearJornadaDto { FechaOperativa = new DateOnly(2026, 1, 20) });
        // Estado ASIGNADO pero sin UnidadOperativaId: inconsistencia que igualmente debe bloquear la publicación.
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 1, JornadaId = jornada.JornadaId, UnidadOperativaId = null, Estado = EstadoServicio.ASIGNADO });

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.PublicarAsync(1, jornada.JornadaId));
    }

    [Fact]
    public async Task PublicarAsync_IgnoraServiciosCancelados()
    {
        var servicioRepo = new ServicioRepositorioFalso();
        var servicio = CrearServicio(new JornadaRepositorioFalso(), servicioRepo);
        var jornada = await servicio.CrearAsync(1, new CrearJornadaDto { FechaOperativa = new DateOnly(2026, 1, 20) });
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 1, JornadaId = jornada.JornadaId, UnidadOperativaId = 1, Estado = EstadoServicio.ASIGNADO });
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 2, JornadaId = jornada.JornadaId, UnidadOperativaId = null, Estado = EstadoServicio.CANCELADO });

        await servicio.PublicarAsync(1, jornada.JornadaId);

        Assert.Equal(EstadoServicio.PUBLICADO, servicioRepo.Servicios[0].Estado);
        Assert.Equal(EstadoServicio.CANCELADO, servicioRepo.Servicios[1].Estado);
    }

    [Fact]
    public async Task PublicarAsync_NotificaAlConductorYAlEmpleadoDelServicioPublicado()
    {
        var servicioRepo = new ServicioRepositorioFalso();
        var unidadRepo = new UnidadOperativaRepositorioFalso(new UnidadOperativa { UnidadOperativaId = 1, ConductorId = 10, VehiculoId = 1, Activa = true });
        var conductorRepo = new ConductorRepositorioFalso(new Conductor { ConductorId = 10, UsuarioId = 100, Activo = true });
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso(new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, EmpleadoId = 20 });
        var empleadoRepo = new EmpleadoRepositorioFalso(new Empleado { EmpleadoId = 20, UsuarioId = 200, EmpresaId = 1, Activo = true });
        var notificacionRepo = new NotificacionRepositorioFalso();
        var jornadaRepo = new JornadaRepositorioFalso();
        var servicio = CrearServicio(jornadaRepo, servicioRepo, unidadRepo, conductorRepo, pasajeroRepo, empleadoRepo, notificacionRepositorio: notificacionRepo);
        var jornada = await servicio.CrearAsync(1, new CrearJornadaDto { FechaOperativa = new DateOnly(2026, 1, 20) });
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 1, JornadaId = jornada.JornadaId, UnidadOperativaId = 1, Estado = EstadoServicio.ASIGNADO });

        await servicio.PublicarAsync(1, jornada.JornadaId);

        Assert.Equal(2, notificacionRepo.Notificaciones.Count);
        Assert.Contains(notificacionRepo.Notificaciones, n => n.UsuarioId == 100);
        Assert.Contains(notificacionRepo.Notificaciones, n => n.UsuarioId == 200);
    }

    [Fact]
    public async Task PublicarAsync_NoNotificaServiciosQueYaEstabanPublicados()
    {
        var servicioRepo = new ServicioRepositorioFalso();
        var unidadRepo = new UnidadOperativaRepositorioFalso(new UnidadOperativa { UnidadOperativaId = 1, ConductorId = 10, VehiculoId = 1, Activa = true });
        var conductorRepo = new ConductorRepositorioFalso(new Conductor { ConductorId = 10, UsuarioId = 100, Activo = true });
        var notificacionRepo = new NotificacionRepositorioFalso();
        var jornadaRepo = new JornadaRepositorioFalso();
        var servicio = CrearServicio(jornadaRepo, servicioRepo, unidadRepo, conductorRepo, notificacionRepositorio: notificacionRepo);
        var jornada = await servicio.CrearAsync(1, new CrearJornadaDto { FechaOperativa = new DateOnly(2026, 1, 20) });
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 1, JornadaId = jornada.JornadaId, UnidadOperativaId = 1, Estado = EstadoServicio.PUBLICADO });

        await servicio.PublicarAsync(1, jornada.JornadaId);

        Assert.Empty(notificacionRepo.Notificaciones);
    }

    [Fact]
    public async Task EliminarRastroAsync_EliminaServiciosPasajerosYProgramacionesSinAsignar_YLaJornadaSiQuedaVacia()
    {
        var jornadaRepo = new JornadaRepositorioFalso();
        var servicioRepo = new ServicioRepositorioFalso();
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso(new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, ProgramacionTransporteId = 1, EmpleadoId = 20 });
        var programacionRepo = new ProgramacionRepositorioFalso();
        programacionRepo.Programaciones.Add(new ProgramacionTransporte { ProgramacionTransporteId = 1, EmpresaId = 1, EmpleadoId = 20, SedeId = 1, Fecha = new DateOnly(2026, 1, 20) });
        // Programación "sin asignar": nunca llegó a tener un ServicioPasajero (por ejemplo, una
        // importación que falló a medias), pero es de la misma fecha y también debe eliminarse.
        programacionRepo.Programaciones.Add(new ProgramacionTransporte { ProgramacionTransporteId = 2, EmpresaId = 1, EmpleadoId = 21, SedeId = 1, Fecha = new DateOnly(2026, 1, 20) });
        var servicio = CrearServicio(jornadaRepo, servicioRepo, servicioPasajeroRepositorio: pasajeroRepo, programacionRepositorio: programacionRepo);
        var jornada = await servicio.CrearAsync(1, new CrearJornadaDto { FechaOperativa = new DateOnly(2026, 1, 20) });
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 1, JornadaId = jornada.JornadaId, Estado = EstadoServicio.ASIGNADO });

        var resultado = await servicio.EliminarRastroAsync(1, jornada.JornadaId);

        Assert.Equal(1, resultado.ServiciosEliminados);
        Assert.Equal(1, resultado.PasajerosEliminados);
        Assert.Equal(2, resultado.ProgramacionesEliminadas);
        Assert.True(resultado.JornadaEliminada);
        Assert.Empty(servicioRepo.Servicios);
        Assert.Empty(programacionRepo.Programaciones);
        Assert.Null(await jornadaRepo.ObtenerPorIdAsync(jornada.JornadaId));
    }

    [Fact]
    public async Task EliminarRastroAsync_LanzaExcepcion_CuandoHayUnServicioPublicado()
    {
        var jornadaRepo = new JornadaRepositorioFalso();
        var servicioRepo = new ServicioRepositorioFalso();
        var servicio = CrearServicio(jornadaRepo, servicioRepo);
        var jornada = await servicio.CrearAsync(1, new CrearJornadaDto { FechaOperativa = new DateOnly(2026, 1, 20) });
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 1, JornadaId = jornada.JornadaId, Estado = EstadoServicio.PUBLICADO });
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 2, JornadaId = jornada.JornadaId, Estado = EstadoServicio.BORRADOR });

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.EliminarRastroAsync(1, jornada.JornadaId));

        // Nada se tocó: la excepción se lanza antes de eliminar cualquier cosa.
        Assert.Equal(2, servicioRepo.Servicios.Count);
    }

    [Fact]
    public async Task EliminarRastroAsync_NoEliminaLaProgramacionNiLaJornada_SiQuedaUnServicioCanceladoConPasajero()
    {
        var jornadaRepo = new JornadaRepositorioFalso();
        var servicioRepo = new ServicioRepositorioFalso();
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso(new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, ProgramacionTransporteId = 1, EmpleadoId = 20 });
        var programacionRepo = new ProgramacionRepositorioFalso();
        programacionRepo.Programaciones.Add(new ProgramacionTransporte { ProgramacionTransporteId = 1, EmpresaId = 1, EmpleadoId = 20, SedeId = 1, Fecha = new DateOnly(2026, 1, 20) });
        var servicio = CrearServicio(jornadaRepo, servicioRepo, servicioPasajeroRepositorio: pasajeroRepo, programacionRepositorio: programacionRepo);
        var jornada = await servicio.CrearAsync(1, new CrearJornadaDto { FechaOperativa = new DateOnly(2026, 1, 20) });
        // El servicio 1 está CANCELADO: DeshacerReparto/EliminarRastro nunca lo tocan (ya es historia), así
        // que su pasajero (y la programación detrás de él) siguen en pie y no deben borrarse.
        servicioRepo.Servicios.Add(new Servicio { ServicioId = 1, JornadaId = jornada.JornadaId, Estado = EstadoServicio.CANCELADO });

        var resultado = await servicio.EliminarRastroAsync(1, jornada.JornadaId);

        Assert.Equal(0, resultado.ServiciosEliminados);
        Assert.Equal(0, resultado.ProgramacionesEliminadas);
        Assert.False(resultado.JornadaEliminada);
        Assert.Single(programacionRepo.Programaciones);
        Assert.NotNull(await jornadaRepo.ObtenerPorIdAsync(jornada.JornadaId));
    }
}
