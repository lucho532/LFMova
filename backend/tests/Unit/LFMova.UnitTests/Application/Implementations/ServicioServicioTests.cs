using LFMova.Application.DTOs.Servicios;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

public class ServicioServicioTests
{
    private class JornadaRepositorioFalso : IJornadaRepositorio
    {
        private readonly Dictionary<int, Jornada> _jornadas;

        public JornadaRepositorioFalso(params Jornada[] jornadas)
            => _jornadas = jornadas.ToDictionary(j => j.JornadaId);

        public Task<Jornada?> ObtenerPorIdAsync(int jornadaId)
            => Task.FromResult(_jornadas.TryGetValue(jornadaId, out var j) ? j : null);

        public Task<List<Jornada>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_jornadas.Values.Where(j => j.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(Jornada jornada) => Task.CompletedTask;

        public Task EliminarAsync(Jornada jornada) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class SedeRepositorioFalso : ISedeRepositorio
    {
        private readonly Dictionary<int, Sede> _sedes;

        public SedeRepositorioFalso(params Sede[] sedes)
            => _sedes = sedes.ToDictionary(s => s.SedeId);

        public Task<Sede?> ObtenerPorIdAsync(int sedeId)
            => Task.FromResult(_sedes.TryGetValue(sedeId, out var s) ? s : null);

        public Task<List<Sede>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_sedes.Values.Where(s => s.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(Sede sede) => Task.CompletedTask;

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

        public Task<List<UnidadOperativa>> ObtenerPorConductorAsync(int conductorId)
            => Task.FromResult(_unidades.Values.Where(u => u.ConductorId == conductorId).ToList());

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

    private class ServicioRepositorioFalso : IServicioRepositorio
    {
        private readonly Dictionary<int, Servicio> _servicios = new();
        private readonly Dictionary<int, Jornada> _jornadasPorId;
        private int _siguienteId = 1;

        public ServicioRepositorioFalso(Dictionary<int, Jornada> jornadasPorId)
            => _jornadasPorId = jornadasPorId;

        public Task<Servicio?> ObtenerPorIdAsync(int servicioId)
        {
            if (!_servicios.TryGetValue(servicioId, out var servicio))
            {
                return Task.FromResult<Servicio?>(null);
            }

            servicio.Jornada = _jornadasPorId.GetValueOrDefault(servicio.JornadaId);
            return Task.FromResult<Servicio?>(servicio);
        }

        public Task<List<Servicio>> ObtenerPorJornadaAsync(int jornadaId)
            => Task.FromResult(_servicios.Values.Where(s => s.JornadaId == jornadaId).ToList());

        public Task<List<Servicio>> ObtenerPorUnidadOperativaAsync(int unidadOperativaId)
            => Task.FromResult(_servicios.Values.Where(s => s.UnidadOperativaId == unidadOperativaId).ToList());

        public Task<List<Servicio>> ObtenerPublicadosPorTipoAsync(TipoServicio tipo)
            => Task.FromResult(_servicios.Values.Where(s => s.Estado == EstadoServicio.PUBLICADO && s.Tipo == tipo).ToList());


        public Task<List<Servicio>> ObtenerPendientesDeProgramacionAsync(int empresaId, DateOnly desde) => Task.FromResult(new List<Servicio>());



        public Task<List<Servicio>> ObtenerActivosPorFechaYHoraAsync(DateOnly fecha, TimeOnly hora) => Task.FromResult(new List<Servicio>());

        public Task AgregarAsync(Servicio servicio)
        {
            servicio.ServicioId = _siguienteId++;
            _servicios[servicio.ServicioId] = servicio;
            return Task.CompletedTask;
        }

        public Task EliminarAsync(Servicio servicio)
        {
            _servicios.Remove(servicio.ServicioId);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class ServicioPasajeroRepositorioFalso : IServicioPasajeroRepositorio
    {
        private readonly List<ServicioPasajero> _pasajeros;

        public ServicioPasajeroRepositorioFalso(params ServicioPasajero[] pasajeros)
            => _pasajeros = pasajeros.ToList();

        public Task<ServicioPasajero?> ObtenerPorIdAsync(int servicioPasajeroId) => Task.FromResult<ServicioPasajero?>(null);

        public Task<ServicioPasajero?> ObtenerPorProgramacionAsync(int programacionTransporteId) => Task.FromResult<ServicioPasajero?>(null);

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

    private const int ConductorVinculadoId = 1;
    private const int UnidadActivaId = 1;

    private static (JornadaRepositorioFalso jornadas, SedeRepositorioFalso sedes, ServicioRepositorioFalso servicios, UnidadOperativaRepositorioFalso unidades, ConductorRepositorioFalso conductores) CrearRepositorios(
        int empresaJornada = 1, int empresaSede = 1, int empresaVinculacionConductor = 1, bool unidadActiva = true)
    {
        var jornada = new Jornada { JornadaId = 1, EmpresaId = empresaJornada, FechaOperativa = new DateOnly(2026, 1, 20) };
        var jornadaRepo = new JornadaRepositorioFalso(jornada);
        var sedeRepo = new SedeRepositorioFalso(new Sede { SedeId = 1, EmpresaId = empresaSede, Activa = true });
        var servicioRepo = new ServicioRepositorioFalso(new Dictionary<int, Jornada> { [1] = jornada });
        var unidadRepo = new UnidadOperativaRepositorioFalso(new UnidadOperativa { UnidadOperativaId = UnidadActivaId, ConductorId = ConductorVinculadoId, VehiculoId = 1, Activa = unidadActiva });
        var conductor = new Conductor
        {
            ConductorId = ConductorVinculadoId,
            UsuarioId = 50,
            Activo = true,
            VinculacionesConductorEmpresa = [new VinculacionConductorEmpresa { ConductorId = ConductorVinculadoId, EmpresaId = empresaVinculacionConductor, Activa = true }]
        };
        var conductorRepo = new ConductorRepositorioFalso(conductor);
        return (jornadaRepo, sedeRepo, servicioRepo, unidadRepo, conductorRepo);
    }

    private static ServicioServicio CrearServicio(
        ServicioRepositorioFalso servicios,
        JornadaRepositorioFalso jornadas,
        SedeRepositorioFalso sedes,
        UnidadOperativaRepositorioFalso unidades,
        ConductorRepositorioFalso conductores,
        ServicioPasajeroRepositorioFalso? pasajeros = null,
        EmpleadoRepositorioFalso? empleados = null,
        NotificacionRepositorioFalso? notificaciones = null)
        => new(
            servicios,
            jornadas,
            sedes,
            unidades,
            conductores,
            pasajeros ?? new ServicioPasajeroRepositorioFalso(),
            empleados ?? new EmpleadoRepositorioFalso(),
            new NotificacionServicio(notificaciones ?? new NotificacionRepositorioFalso()));

    private static CrearServicioDto DtoValido(int? unidadOperativaId = null) => new()
    {
        UnidadOperativaId = unidadOperativaId,
        SedeId = 1,
        Fecha = new DateOnly(2026, 1, 20),
        HoraProgramada = new TimeOnly(7, 0),
        Tipo = TipoServicio.ENTRADA
    };

    [Fact]
    public async Task CrearAsync_CreaEnEstadoBorrador_CuandoJornadaYSedePertenecenALaEmpresa()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);

        var creado = await servicio.CrearAsync(1, 1, DtoValido());

        Assert.Equal(EstadoServicio.BORRADOR, creado.Estado);
        Assert.Equal(1, creado.JornadaId);
        Assert.Null(creado.UnidadOperativaId);
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoLaJornadaEsDeOtraEmpresa()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios(empresaJornada: 2);
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearAsync(1, 1, DtoValido()));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoLaSedeEsDeOtraEmpresa()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios(empresaSede: 2);
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearAsync(1, 1, DtoValido()));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoLaFechaNoSeEstablece()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);

        var datos = DtoValido();
        datos.Fecha = default;

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearAsync(1, 1, datos));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoElTipoNoEsValido()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);

        var datos = DtoValido();
        datos.Tipo = (TipoServicio)99;

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearAsync(1, 1, datos));
    }

    [Fact]
    public async Task CrearAsync_AsignaLaUnidad_CuandoSeIndicaUnaValida()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);

        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));

        Assert.Equal(UnidadActivaId, creado.UnidadOperativaId);
    }

    [Fact]
    public async Task CambiarEstadoAsync_Avanza_CuandoLaTransicionEsValida()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido());

        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
        var actualizado = await servicio.ObtenerPorIdAsync(1, creado.ServicioId);

        Assert.Equal(EstadoServicio.PENDIENTE_ASIGNACION, actualizado!.Estado);
    }

    [Fact]
    public async Task CambiarEstadoAsync_LanzaExcepcion_CuandoLaTransicionNoEsValida()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PUBLICADO }));
    }

    [Fact]
    public async Task ObtenerPorIdAsync_DevuelveNulo_CuandoElServicioEsDeOtraEmpresa()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido());

        var resultado = await servicio.ObtenerPorIdAsync(2, creado.ServicioId);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task AsignarUnidadAsync_Asigna_CuandoLaUnidadEstaActivaYVinculada()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido());

        await servicio.AsignarUnidadAsync(1, creado.ServicioId, new AsignarUnidadServicioDto { UnidadOperativaId = UnidadActivaId });
        var actualizado = await servicio.ObtenerPorIdAsync(1, creado.ServicioId);

        Assert.Equal(UnidadActivaId, actualizado!.UnidadOperativaId);
    }

    [Fact]
    public async Task AsignarUnidadAsync_TransicionaAAsignado_CuandoElServicioEstabaPendienteDeAsignacion()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido());
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });

        await servicio.AsignarUnidadAsync(1, creado.ServicioId, new AsignarUnidadServicioDto { UnidadOperativaId = UnidadActivaId });
        var actualizado = await servicio.ObtenerPorIdAsync(1, creado.ServicioId);

        Assert.Equal(EstadoServicio.ASIGNADO, actualizado!.Estado);
    }

    [Fact]
    public async Task AsignarUnidadAsync_ConservaJornadaId_AlReasignar()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));

        // Reasignar a la misma unidad (no hay una segunda unidad de prueba aquí);
        // lo relevante es que JornadaId nunca se toca por esta operación.
        await servicio.AsignarUnidadAsync(1, creado.ServicioId, new AsignarUnidadServicioDto { UnidadOperativaId = UnidadActivaId });
        var actualizado = await servicio.ObtenerPorIdAsync(1, creado.ServicioId);

        Assert.Equal(1, actualizado!.JornadaId);
    }

    [Fact]
    public async Task AsignarUnidadAsync_LanzaExcepcion_CuandoLaUnidadNoEstaActiva()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios(unidadActiva: false);
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.AsignarUnidadAsync(1, creado.ServicioId, new AsignarUnidadServicioDto { UnidadOperativaId = UnidadActivaId }));
    }

    [Fact]
    public async Task AsignarUnidadAsync_LanzaExcepcion_CuandoElConductorNoTieneVinculacionActivaConLaEmpresa()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios(empresaVinculacionConductor: 2);
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.AsignarUnidadAsync(1, creado.ServicioId, new AsignarUnidadServicioDto { UnidadOperativaId = UnidadActivaId }));
    }

    [Fact]
    public async Task AsignarUnidadAsync_LanzaExcepcion_CuandoHayConflictoTemporalConOtroServicioDeLaMismaUnidad()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var existente = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));
        var nuevo = await servicio.CrearAsync(1, 1, DtoValido());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.AsignarUnidadAsync(1, nuevo.ServicioId, new AsignarUnidadServicioDto { UnidadOperativaId = UnidadActivaId }));
    }

    [Fact]
    public async Task AsignarUnidadAsync_NoConsideraConflicto_CuandoElOtroServicioEstaCancelado()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var existente = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));
        await servicio.CambiarEstadoAsync(1, existente.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.CANCELADO });
        var nuevo = await servicio.CrearAsync(1, 1, DtoValido());

        await servicio.AsignarUnidadAsync(1, nuevo.ServicioId, new AsignarUnidadServicioDto { UnidadOperativaId = UnidadActivaId });
        var actualizado = await servicio.ObtenerPorIdAsync(1, nuevo.ServicioId);

        Assert.Equal(UnidadActivaId, actualizado!.UnidadOperativaId);
    }

    private static async Task<int> CrearServicioPublicadoAsync(ServicioServicio servicio, int unidadOperativaId)
    {
        var creado = await servicio.CrearAsync(1, 1, DtoValido(unidadOperativaId));
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.ASIGNADO });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PUBLICADO });
        return creado.ServicioId;
    }

    [Fact]
    public async Task DespublicarAsync_VuelveAAsignado_YConservaLaUnidad()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var servicioId = await CrearServicioPublicadoAsync(servicio, UnidadActivaId);

        await servicio.DespublicarAsync(1, servicioId);
        var actualizado = await servicio.ObtenerPorIdAsync(1, servicioId);

        Assert.Equal(EstadoServicio.ASIGNADO, actualizado!.Estado);
        Assert.Equal(UnidadActivaId, actualizado.UnidadOperativaId);
    }

    [Fact]
    public async Task DespublicarAsync_NotificaAlConductor()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var notificaciones = new NotificacionRepositorioFalso();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores, notificaciones: notificaciones);
        var servicioId = await CrearServicioPublicadoAsync(servicio, UnidadActivaId);

        await servicio.DespublicarAsync(1, servicioId);

        var notificacion = Assert.Single(notificaciones.Notificaciones);
        Assert.Equal(50, notificacion.UsuarioId);
        Assert.Equal("RUTA_MODIFICADA", notificacion.Tipo);
    }

    [Fact]
    public async Task DespublicarAsync_LanzaExcepcion_CuandoElServicioNoEstaPublicado()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.DespublicarAsync(1, creado.ServicioId));
    }

    [Fact]
    public async Task EliminarAsync_BorraElServicioYSusPasajeros()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var pasajeros = new ServicioPasajeroRepositorioFalso();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores, pasajeros: pasajeros);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));
        await pasajeros.AgregarAsync(new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = creado.ServicioId, EmpleadoId = 1 });

        await servicio.EliminarAsync(1, creado.ServicioId);

        Assert.Null(await servicio.ObtenerPorIdAsync(1, creado.ServicioId));
        Assert.Empty(await pasajeros.ObtenerPorServicioAsync(creado.ServicioId));
    }

    [Fact]
    public async Task EliminarAsync_NotificaAlConductor_CuandoLaRutaEstabaPublicada()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var notificaciones = new NotificacionRepositorioFalso();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores, notificaciones: notificaciones);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));
        (await servicios.ObtenerPorIdAsync(creado.ServicioId))!.Estado = EstadoServicio.PUBLICADO;

        await servicio.EliminarAsync(1, creado.ServicioId);

        var notificacion = Assert.Single(notificaciones.Notificaciones);
        Assert.Equal(50, notificacion.UsuarioId);
        Assert.Equal("RUTA_MODIFICADA", notificacion.Tipo);
    }

    [Fact]
    public async Task EliminarAsync_NoNotifica_CuandoLaRutaNoSeHabiaPublicado()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var notificaciones = new NotificacionRepositorioFalso();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores, notificaciones: notificaciones);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));

        await servicio.EliminarAsync(1, creado.ServicioId);

        Assert.Empty(notificaciones.Notificaciones);
    }

    [Theory]
    [InlineData(EstadoServicio.EN_CURSO)]
    [InlineData(EstadoServicio.FINALIZADO)]
    public async Task EliminarAsync_LanzaExcepcion_CuandoEstaEnCursoOFinalizado(EstadoServicio estado)
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));
        var entidad = await servicios.ObtenerPorIdAsync(creado.ServicioId);
        entidad!.Estado = estado;

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.EliminarAsync(1, creado.ServicioId));
    }

    [Fact]
    public async Task AsignarUnidadAsync_NotificaAlNuevoConductorYAlEmpleado_CuandoCambiaElConductor()
    {
        const int SegundaUnidadId = 2;
        const int SegundoConductorId = 2;
        var jornada = new Jornada { JornadaId = 1, EmpresaId = 1, FechaOperativa = new DateOnly(2026, 1, 20) };
        var jornadas = new JornadaRepositorioFalso(jornada);
        var sedes = new SedeRepositorioFalso(new Sede { SedeId = 1, EmpresaId = 1, Activa = true });
        var servicios = new ServicioRepositorioFalso(new Dictionary<int, Jornada> { [1] = jornada });
        var unidades = new UnidadOperativaRepositorioFalso(
            new UnidadOperativa { UnidadOperativaId = UnidadActivaId, ConductorId = ConductorVinculadoId, VehiculoId = 1, Activa = true },
            new UnidadOperativa { UnidadOperativaId = SegundaUnidadId, ConductorId = SegundoConductorId, VehiculoId = 2, Activa = true });
        var conductores = new ConductorRepositorioFalso(
            new Conductor { ConductorId = ConductorVinculadoId, UsuarioId = 50, Activo = true, VinculacionesConductorEmpresa = [new VinculacionConductorEmpresa { ConductorId = ConductorVinculadoId, EmpresaId = 1, Activa = true }] },
            new Conductor { ConductorId = SegundoConductorId, UsuarioId = 60, Activo = true, VinculacionesConductorEmpresa = [new VinculacionConductorEmpresa { ConductorId = SegundoConductorId, EmpresaId = 1, Activa = true }] });
        // ServicioRepositorioFalso asigna ids autoincrementales desde 1; al ser el único
        // servicio creado en este repositorio, su ServicioId será 1.
        var pasajeros = new ServicioPasajeroRepositorioFalso(new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, EmpleadoId = 1 });
        var empleados = new EmpleadoRepositorioFalso(new Empleado { EmpleadoId = 1, UsuarioId = 300, EmpresaId = 1, Activo = true });
        var notificaciones = new NotificacionRepositorioFalso();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores, pasajeros, empleados, notificaciones);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));

        // Mientras la ruta no está publicada, cambiarle el conductor es trabajo interno: no se avisa a nadie.
        await servicio.AsignarUnidadAsync(1, creado.ServicioId, new AsignarUnidadServicioDto { UnidadOperativaId = SegundaUnidadId });
        Assert.Empty(notificaciones.Notificaciones);

        // Ya publicada, el cambio sí se avisa al nuevo conductor y a los pasajeros (no al anterior).
        (await servicios.ObtenerPorIdAsync(creado.ServicioId))!.Estado = EstadoServicio.PUBLICADO;
        await servicio.AsignarUnidadAsync(1, creado.ServicioId, new AsignarUnidadServicioDto { UnidadOperativaId = UnidadActivaId });

        Assert.Equal(2, notificaciones.Notificaciones.Count);
        Assert.Contains(notificaciones.Notificaciones, n => n.UsuarioId == 50 && n.Mensaje.StartsWith("Se te asignó la ruta de entrada de "));
        Assert.Contains(notificaciones.Notificaciones, n => n.UsuarioId == 300);
        Assert.DoesNotContain(notificaciones.Notificaciones, n => n.UsuarioId == 60);
        Assert.DoesNotContain(notificaciones.Notificaciones, n => n.Mensaje.Contains($"servicio {creado.ServicioId}"));
    }

    [Fact]
    public async Task AsignarUnidadAsync_NoNotifica_CuandoEsLaPrimeraAsignacion()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var notificaciones = new NotificacionRepositorioFalso();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores, notificaciones: notificaciones);
        var creado = await servicio.CrearAsync(1, 1, DtoValido());

        await servicio.AsignarUnidadAsync(1, creado.ServicioId, new AsignarUnidadServicioDto { UnidadOperativaId = UnidadActivaId });

        Assert.Empty(notificaciones.Notificaciones);
    }

    [Fact]
    public async Task AsignarUnidadAsync_NoNotifica_CuandoElConductorNoCambia()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var notificaciones = new NotificacionRepositorioFalso();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores, notificaciones: notificaciones);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));

        await servicio.AsignarUnidadAsync(1, creado.ServicioId, new AsignarUnidadServicioDto { UnidadOperativaId = UnidadActivaId });

        Assert.Empty(notificaciones.Notificaciones);
    }

    [Fact]
    public async Task ObtenerUsuarioIdConductorAsignadoAsync_DevuelveElUsuarioIdDelConductorDeLaUnidad()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));

        var usuarioId = await servicio.ObtenerUsuarioIdConductorAsignadoAsync(1, creado.ServicioId);

        Assert.Equal(50, usuarioId);
    }

    [Fact]
    public async Task ObtenerUsuarioIdConductorAsignadoAsync_DevuelveNulo_CuandoElServicioNoTieneUnidadAsignada()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido());

        var usuarioId = await servicio.ObtenerUsuarioIdConductorAsignadoAsync(1, creado.ServicioId);

        Assert.Null(usuarioId);
    }

    [Fact]
    public async Task IniciarAsync_PasaAEnCurso_YRegistraLaHoraDeInicio()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.ASIGNADO });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PUBLICADO });

        await servicio.IniciarAsync(1, creado.ServicioId);
        var actualizado = await servicio.ObtenerPorIdAsync(1, creado.ServicioId);

        Assert.Equal(EstadoServicio.EN_CURSO, actualizado!.Estado);
        Assert.NotNull(actualizado.HoraInicioReal);
    }

    [Fact]
    public async Task IniciarAsync_GuardaLaUbicacionDelConductor_CuandoSeIndica()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.ASIGNADO });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PUBLICADO });

        await servicio.IniciarAsync(1, creado.ServicioId, new IniciarServicioDto { Latitud = 4.65, Longitud = -74.05 });
        var actualizado = await servicio.ObtenerPorIdAsync(1, creado.ServicioId);

        Assert.Equal(4.65, actualizado!.LatitudInicio);
        Assert.Equal(-74.05, actualizado.LongitudInicio);
    }

    [Fact]
    public async Task IniciarAsync_LanzaExcepcion_CuandoElServicioNoEstaPublicado()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.IniciarAsync(1, creado.ServicioId));
    }

    private async Task<(ServicioServicio servicio, ServicioDto creado)> CrearServicioEnCursoAsync(TipoServicio tipo = TipoServicio.ENTRADA)
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));
        if (tipo != TipoServicio.ENTRADA)
        {
            // DtoValido crea el servicio en ENTRADA; para SALIDA se ajusta directamente sobre la entidad ya creada.
            var entidad = await servicios.ObtenerPorIdAsync(creado.ServicioId);
            entidad!.Tipo = TipoServicio.SALIDA;
        }

        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.ASIGNADO });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PUBLICADO });
        await servicio.IniciarAsync(1, creado.ServicioId);

        return (servicio, creado);
    }

    [Fact]
    public async Task FinalizarAsync_Finaliza_CuandoLaEntradaNoTienePasajerosPendientes()
    {
        var (servicio, creado) = await CrearServicioEnCursoAsync();

        await servicio.FinalizarAsync(1, creado.ServicioId);
        var actualizado = await servicio.ObtenerPorIdAsync(1, creado.ServicioId);

        Assert.Equal(EstadoServicio.FINALIZADO, actualizado!.Estado);
        Assert.NotNull(actualizado.HoraFinReal);
    }

    [Fact]
    public async Task FinalizarAsync_GuardaLaUbicacionDelConductor_CuandoSeIndica()
    {
        var (servicio, creado) = await CrearServicioEnCursoAsync();

        await servicio.FinalizarAsync(1, creado.ServicioId, new FinalizarServicioDto { Latitud = 4.65, Longitud = -74.05 });
        var actualizado = await servicio.ObtenerPorIdAsync(1, creado.ServicioId);

        Assert.Equal(4.65, actualizado!.LatitudFinalizacion);
        Assert.Equal(-74.05, actualizado.LongitudFinalizacion);
    }

    [Fact]
    public async Task FinalizarAsync_Finaliza_ParaSalida_SinValidarPasajeros()
    {
        var (servicio, creado) = await CrearServicioEnCursoAsync(TipoServicio.SALIDA);

        await servicio.FinalizarAsync(1, creado.ServicioId);
        var actualizado = await servicio.ObtenerPorIdAsync(1, creado.ServicioId);

        Assert.Equal(EstadoServicio.FINALIZADO, actualizado!.Estado);
    }

    [Fact]
    public async Task FinalizarAsync_LanzaExcepcion_CuandoHayPasajerosPendientesParaEntrada()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var pasajeros = new ServicioPasajeroRepositorioFalso(
            new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, EmpleadoId = 1, Estado = EstadoServicioPasajero.PROGRAMADO });
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores, pasajeros: pasajeros);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.ASIGNADO });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PUBLICADO });
        await servicio.IniciarAsync(1, creado.ServicioId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.FinalizarAsync(1, creado.ServicioId));
    }

    [Fact]
    public async Task FinalizarAsync_LanzaExcepcion_CuandoElServicioNoEstaEnCurso()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.FinalizarAsync(1, creado.ServicioId));
    }
}
