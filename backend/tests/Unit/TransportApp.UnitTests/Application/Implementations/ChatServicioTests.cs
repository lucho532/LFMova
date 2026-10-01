using TransportApp.Application.DTOs.Chat;
using TransportApp.Application.Implementations;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;

namespace TransportApp.UnitTests.Application.Implementations;

public class ChatServicioTests
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

        public Task<List<int>> ObtenerConductorIdsPorEmpleadoAsync(int empleadoId) => Task.FromResult(new List<int>());

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

        public Task<List<Empleado>> ObtenerPorEmpresaAsync(int empresaId) => Task.FromResult(new List<Empleado>());

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

    private class ConversacionRepositorioFalso : IConversacionRepositorio
    {
        public readonly List<Conversacion> Conversaciones = new();
        private int _siguienteId = 1;

        public Task<Conversacion?> ObtenerPorServicioPasajeroAsync(int servicioPasajeroId)
            => Task.FromResult(Conversaciones.FirstOrDefault(c => c.ServicioPasajeroId == servicioPasajeroId));

        public Task AgregarAsync(Conversacion conversacion)
        {
            conversacion.ConversacionId = _siguienteId++;
            Conversaciones.Add(conversacion);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class MensajeRepositorioFalso : IMensajeRepositorio
    {
        public readonly List<Mensaje> Mensajes = new();
        private int _siguienteId = 1;

        public Task<List<Mensaje>> ObtenerPorConversacionAsync(int conversacionId)
            => Task.FromResult(Mensajes.Where(m => m.ConversacionId == conversacionId).OrderBy(m => m.FechaHora).ToList());

        public Task AgregarAsync(Mensaje mensaje)
        {
            mensaje.MensajeId = _siguienteId++;
            Mensajes.Add(mensaje);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private const int EmpresaId = 1;
    private const int UsuarioIdEmpleado = 100;
    private const int UsuarioIdConductor = 200;
    private const int UsuarioIdAjeno = 999;

    private (ChatServicio servicio, ConversacionRepositorioFalso conversaciones, MensajeRepositorioFalso mensajes) CrearServicio(bool conServicioAsignado = true)
    {
        var jornada = new Jornada { JornadaId = 1, EmpresaId = EmpresaId, FechaOperativa = new DateOnly(2026, 1, 20) };
        var servicioEntidad = new Servicio
        {
            ServicioId = 1, JornadaId = 1, UnidadOperativaId = conServicioAsignado ? 1 : null, Jornada = jornada
        };
        var servicioRepo = new ServicioRepositorioFalso(servicioEntidad);
        var pasajero = new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, EmpleadoId = 1 };
        var pasajeroRepo = new ServicioPasajeroRepositorioFalso(pasajero);
        var empleadoRepo = new EmpleadoRepositorioFalso(new Empleado { EmpleadoId = 1, UsuarioId = UsuarioIdEmpleado, EmpresaId = EmpresaId, Activo = true });
        var unidadRepo = new UnidadOperativaRepositorioFalso(new UnidadOperativa { UnidadOperativaId = 1, ConductorId = 1, VehiculoId = 1, Activa = true });
        var conductorRepo = new ConductorRepositorioFalso(new Conductor { ConductorId = 1, UsuarioId = UsuarioIdConductor, Activo = true });
        var conversacionRepo = new ConversacionRepositorioFalso();
        var mensajeRepo = new MensajeRepositorioFalso();

        var servicio = new ChatServicio(pasajeroRepo, servicioRepo, empleadoRepo, unidadRepo, conductorRepo, conversacionRepo, mensajeRepo);

        return (servicio, conversacionRepo, mensajeRepo);
    }

    [Fact]
    public async Task ObtenerParticipantesAsync_DevuelveElEmpleadoYElConductor()
    {
        var (servicio, _, _) = CrearServicio();

        var (usuarioIdEmpleado, usuarioIdConductor) = await servicio.ObtenerParticipantesAsync(EmpresaId, 1);

        Assert.Equal(UsuarioIdEmpleado, usuarioIdEmpleado);
        Assert.Equal(UsuarioIdConductor, usuarioIdConductor);
    }

    [Fact]
    public async Task ObtenerParticipantesAsync_ConductorEsNulo_CuandoElServicioNoTieneUnidadAsignada()
    {
        var (servicio, _, _) = CrearServicio(conServicioAsignado: false);

        var (usuarioIdEmpleado, usuarioIdConductor) = await servicio.ObtenerParticipantesAsync(EmpresaId, 1);

        Assert.Equal(UsuarioIdEmpleado, usuarioIdEmpleado);
        Assert.Null(usuarioIdConductor);
    }

    [Fact]
    public async Task EnviarMensajeAsync_CreaLaConversacionYElMensaje_CuandoElRemitenteEsElEmpleado()
    {
        var (servicio, conversaciones, mensajes) = CrearServicio();

        var mensaje = await servicio.EnviarMensajeAsync(EmpresaId, 1, UsuarioIdEmpleado, new EnviarMensajeDto { Contenido = "¿Dónde estás?" });

        Assert.Single(conversaciones.Conversaciones);
        Assert.Single(mensajes.Mensajes);
        Assert.Equal("¿Dónde estás?", mensaje.Contenido);
        Assert.Equal(UsuarioIdEmpleado, mensaje.UsuarioId);
    }

    [Fact]
    public async Task EnviarMensajeAsync_PermiteAlConductorResponderEnLaMismaConversacion()
    {
        var (servicio, conversaciones, _) = CrearServicio();
        await servicio.EnviarMensajeAsync(EmpresaId, 1, UsuarioIdEmpleado, new EnviarMensajeDto { Contenido = "¿Dónde estás?" });

        await servicio.EnviarMensajeAsync(EmpresaId, 1, UsuarioIdConductor, new EnviarMensajeDto { Contenido = "Ya llego." });

        Assert.Single(conversaciones.Conversaciones);
        var mensajesDeLaConversacion = await servicio.ObtenerMensajesAsync(EmpresaId, 1);
        Assert.Equal(2, mensajesDeLaConversacion.Count);
        Assert.Equal(UsuarioIdConductor, mensajesDeLaConversacion[1].UsuarioId);
    }

    [Fact]
    public async Task EnviarMensajeAsync_LanzaExcepcion_CuandoElRemitenteNoParticipaEnLaConversacion()
    {
        var (servicio, _, _) = CrearServicio();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.EnviarMensajeAsync(EmpresaId, 1, UsuarioIdAjeno, new EnviarMensajeDto { Contenido = "Hola" }));
    }

    [Fact]
    public async Task ObtenerMensajesAsync_DevuelveListaVacia_CuandoNoExisteConversacionTodavia()
    {
        var (servicio, _, _) = CrearServicio();

        var mensajes = await servicio.ObtenerMensajesAsync(EmpresaId, 1);

        Assert.Empty(mensajes);
    }
}
