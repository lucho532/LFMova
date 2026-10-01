using TransportApp.Application.Implementations;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Domain.Rules;

namespace TransportApp.UnitTests.Application.Implementations;

public class AlertaEjecucionServicioTests
{
    private class ServicioRepositorioFalso : IServicioRepositorio
    {
        private readonly List<Servicio> _servicios;

        public ServicioRepositorioFalso(params Servicio[] servicios)
            => _servicios = servicios.ToList();

        public Task<Servicio?> ObtenerPorIdAsync(int servicioId) => Task.FromResult(_servicios.FirstOrDefault(s => s.ServicioId == servicioId));

        public Task<List<Servicio>> ObtenerPorJornadaAsync(int jornadaId) => Task.FromResult(new List<Servicio>());

        public Task<List<Servicio>> ObtenerPorUnidadOperativaAsync(int unidadOperativaId) => Task.FromResult(new List<Servicio>());

        public Task<List<Servicio>> ObtenerPublicadosPorTipoAsync(TipoServicio tipo)
            => Task.FromResult(_servicios.Where(s => s.Estado == EstadoServicio.PUBLICADO && s.Tipo == tipo).ToList());

        public Task<List<Servicio>> ObtenerPendientesDeProgramacionAsync(int empresaId, DateOnly desde) => Task.FromResult(new List<Servicio>());


        public Task<List<Servicio>> ObtenerActivosPorFechaYHoraAsync(DateOnly fecha, TimeOnly hora)
            => Task.FromResult(_servicios.Where(s => s.Fecha == fecha && s.HoraProgramada == hora
                && (s.Estado == EstadoServicio.PUBLICADO || s.Estado == EstadoServicio.EN_CURSO)).ToList());

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

    private const int EmpresaId = 1;

    /// <summary>Instante de reloj de Colombia, que es como están guardadas la fecha y hora de las rutas.</summary>
    private static DateTime AhoraColombia() => ReglasHoraColombia.AhoraColombia(DateTime.UtcNow);

    private static Servicio Ruta(int servicioId, int? unidadOperativaId, DateTime programadaColombia, TipoServicio tipo = TipoServicio.ENTRADA,
        EstadoServicio estado = EstadoServicio.PUBLICADO, int empresaId = EmpresaId) => new()
    {
        ServicioId = servicioId, JornadaId = 10 + empresaId, Tipo = tipo, Estado = estado, UnidadOperativaId = unidadOperativaId,
        // La fecha se toma del mismo instante que la hora: cerca de la medianoche, la ruta puede caer al día siguiente.
        Fecha = DateOnly.FromDateTime(programadaColombia), HoraProgramada = TimeOnly.FromDateTime(programadaColombia),
        Jornada = new Jornada { JornadaId = 10 + empresaId, EmpresaId = empresaId },
        Sede = new Sede { SedeId = 1, Nombre = "La Patria" }
    };

    private static UnidadOperativa Unidad(int id, int conductorId) => new() { UnidadOperativaId = id, ConductorId = conductorId, VehiculoId = id, Activa = true };

    private static (AlertaEjecucionServicio Servicio, NotificacionRepositorioFalso Notificaciones) Crear(Servicio[] rutas, UnidadOperativa[] unidades, Conductor[] conductores)
    {
        var notificaciones = new NotificacionRepositorioFalso();
        var servicio = new AlertaEjecucionServicio(
            new ServicioRepositorioFalso(rutas), new UnidadOperativaRepositorioFalso(unidades), new ConductorRepositorioFalso(conductores),
            new NotificacionServicio(notificaciones));
        return (servicio, notificaciones);
    }

    private static readonly Conductor Andres = new() { ConductorId = 1, UsuarioId = 100, NombreCompleto = "andres felipe otalvaro", Telefono = "3001112233", Activo = true };
    private static readonly Conductor Beatriz = new() { ConductorId = 2, UsuarioId = 200, NombreCompleto = "Beatriz Gómez", Telefono = "3004445566", Activo = true };
    private static readonly Conductor Carlos = new() { ConductorId = 3, UsuarioId = 300, NombreCompleto = "Carlos Ruiz", Telefono = "", Activo = true };

    [Fact]
    public async Task VerificarRutasNoIniciadasAsync_NotificaAlConductor_ConLaHoraYLaSedeDeLaRuta()
    {
        var programada = AhoraColombia().AddMinutes(30);
        var ruta = Ruta(1, 1, programada);
        var (servicio, notificaciones) = Crear(new[] { ruta }, new[] { Unidad(1, 1) }, new[] { Andres });

        await servicio.VerificarRutasNoIniciadasAsync();

        var aviso = Assert.Single(notificaciones.Notificaciones);
        Assert.Equal(100, aviso.UsuarioId);
        Assert.Equal("ALERTA_RUTA_NO_INICIADA", aviso.Tipo);
        Assert.Contains($"Todavía no has iniciado tu ruta de entrada de {TransportApp.Application.Utils.FormatoOperacion.HoraConArticulo(ruta.HoraProgramada)} a La Patria ({ruta.Fecha:dd/MM/yyyy})", aviso.Mensaje);
        Assert.Equal($"/conductor/servicios/{EmpresaId}/{ruta.JornadaId}/1", aviso.Enlace);
    }

    [Fact]
    public async Task VerificarRutasNoIniciadasAsync_UsaLaHoraDeColombia_NoLaHoraUtc()
    {
        // Una ruta a media hora según el reloj UTC está en realidad a 5 horas y media en Colombia: todavía no toca avisar.
        var (servicio, notificaciones) = Crear(
            new[] { Ruta(1, 1, DateTime.UtcNow.AddMinutes(30)) }, new[] { Unidad(1, 1) }, new[] { Andres });

        await servicio.VerificarRutasNoIniciadasAsync();

        Assert.Empty(notificaciones.Notificaciones);
    }

    [Fact]
    public async Task VerificarRutasNoIniciadasAsync_AvisaALosConductoresDeLaMismaEmpresaYHorario_ConNombreYTelefono()
    {
        var programada = AhoraColombia().AddMinutes(30);
        var rutas = new[]
        {
            Ruta(1, 1, programada),                                      // Andrés: no ha iniciado.
            Ruta(2, 2, programada, estado: EstadoServicio.EN_CURSO),     // Beatriz: mismo horario, ya en curso.
            Ruta(3, 3, programada, empresaId: 2),                       // Carlos: mismo horario pero otra empresa.
        };
        var (servicio, notificaciones) = Crear(rutas, new[] { Unidad(1, 1), Unidad(2, 2), Unidad(3, 3) }, new[] { Andres, Beatriz, Carlos });

        await servicio.VerificarRutasNoIniciadasAsync();

        var paraBeatriz = Assert.Single(notificaciones.Notificaciones, n => n.UsuarioId == 200);
        Assert.StartsWith("Andres Felipe Otalvaro no ha iniciado la ruta de entrada de", paraBeatriz.Mensaje);
        Assert.Contains("Su teléfono: 3001112233.", paraBeatriz.Mensaje);
        Assert.Null(paraBeatriz.Enlace);
        Assert.Contains("compañeros de ese horario también reciben este aviso", Assert.Single(notificaciones.Notificaciones, n => n.UsuarioId == 100).Mensaje);
        // Carlos es de otra empresa: no se le avisa, aunque (como su ruta sigue publicada) sí recibe su propia alerta.
        Assert.DoesNotContain(notificaciones.Notificaciones, n => n.UsuarioId == 300 && n.Mensaje.Contains("Andres"));
    }

    [Fact]
    public async Task VerificarRutasNoIniciadasAsync_NoRepiteElAvisoEnMenosDeDiezMinutos()
    {
        var programada = AhoraColombia().AddMinutes(30);
        var (servicio, notificaciones) = Crear(
            new[] { Ruta(1, 1, programada), Ruta(2, 2, programada, estado: EstadoServicio.EN_CURSO) },
            new[] { Unidad(1, 1), Unidad(2, 2) }, new[] { Andres, Beatriz });

        await servicio.VerificarRutasNoIniciadasAsync();
        await servicio.VerificarRutasNoIniciadasAsync();

        Assert.Equal(2, notificaciones.Notificaciones.Count);
    }

    [Fact]
    public async Task VerificarRutasNoIniciadasAsync_VuelveAAvisarEnCadaVerificacion_MientrasSigaSinIniciar()
    {
        var programada = AhoraColombia().AddMinutes(30);
        var (servicio, notificaciones) = Crear(
            new[] { Ruta(1, 1, programada), Ruta(2, 2, programada, estado: EstadoServicio.EN_CURSO) },
            new[] { Unidad(1, 1), Unidad(2, 2) }, new[] { Andres, Beatriz });

        await servicio.VerificarRutasNoIniciadasAsync();
        // Simula que pasaron 15 minutos desde los primeros avisos (la siguiente verificación).
        foreach (var n in notificaciones.Notificaciones)
        {
            n.FechaHora = DateTime.UtcNow.AddMinutes(-15);
        }
        await servicio.VerificarRutasNoIniciadasAsync();

        Assert.Equal(2, notificaciones.Notificaciones.Count(n => n.UsuarioId == 100));
        Assert.Equal(2, notificaciones.Notificaciones.Count(n => n.UsuarioId == 200));
    }

    [Fact]
    public async Task VerificarRutasNoIniciadasAsync_IndicaCuantoFalta()
    {
        var (servicio, notificaciones) = Crear(new[] { Ruta(1, 1, AhoraColombia().AddMinutes(30)) }, new[] { Unidad(1, 1) }, new[] { Andres });

        await servicio.VerificarRutasNoIniciadasAsync();

        Assert.Matches(@"empieza en (30|31) min", Assert.Single(notificaciones.Notificaciones).Mensaje);
    }

    [Fact]
    public async Task VerificarRutasNoIniciadasAsync_NoAvisa_CuandoYaPasoLaHoraDeEntrada()
    {
        var (servicio, notificaciones) = Crear(new[] { Ruta(1, 1, AhoraColombia().AddMinutes(-20)) }, new[] { Unidad(1, 1) }, new[] { Andres });

        await servicio.VerificarRutasNoIniciadasAsync();

        Assert.Empty(notificaciones.Notificaciones);
    }

    [Fact]
    public async Task VerificarRutasNoIniciadasAsync_NoNotifica_CuandoFaltaMasDeUnaHora()
    {
        var (servicio, notificaciones) = Crear(new[] { Ruta(1, 1, AhoraColombia().AddHours(3)) }, new[] { Unidad(1, 1) }, new[] { Andres });

        await servicio.VerificarRutasNoIniciadasAsync();

        Assert.Empty(notificaciones.Notificaciones);
    }

    [Fact]
    public async Task VerificarRutasNoIniciadasAsync_IgnoraServiciosSinUnidadAsignada()
    {
        var (servicio, notificaciones) = Crear(new[] { Ruta(1, null, AhoraColombia().AddMinutes(30)) }, Array.Empty<UnidadOperativa>(), Array.Empty<Conductor>());

        await servicio.VerificarRutasNoIniciadasAsync();

        Assert.Empty(notificaciones.Notificaciones);
    }

    [Fact]
    public async Task VerificarRutasNoIniciadasAsync_IgnoraServiciosDeSalida()
    {
        var (servicio, notificaciones) = Crear(
            new[] { Ruta(1, 1, AhoraColombia().AddMinutes(30), tipo: TipoServicio.SALIDA) }, new[] { Unidad(1, 1) }, new[] { Andres });

        await servicio.VerificarRutasNoIniciadasAsync();

        Assert.Empty(notificaciones.Notificaciones);
    }
}
