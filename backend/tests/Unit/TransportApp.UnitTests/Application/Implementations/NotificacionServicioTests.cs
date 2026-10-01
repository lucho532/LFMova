using TransportApp.Application.Implementations;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;

namespace TransportApp.UnitTests.Application.Implementations;

public class NotificacionServicioTests
{
    private class NotificacionRepositorioFalso : INotificacionRepositorio
    {
        public readonly List<Notificacion> Notificaciones = new();
        private int _siguienteId = 1;

        public Task<List<Notificacion>> ObtenerPorUsuarioAsync(int usuarioId)
            => Task.FromResult(Notificaciones
                .Where(n => n.UsuarioId == usuarioId)
                .OrderByDescending(n => n.FechaHora)
                .ToList());

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

    private const int UsuarioId = 1;

    private static (NotificacionServicio servicio, NotificacionRepositorioFalso repositorio) CrearServicio()
    {
        var repositorio = new NotificacionRepositorioFalso();
        return (new NotificacionServicio(repositorio), repositorio);
    }

    [Fact]
    public async Task CrearAsync_AgregaNotificacionNoLeida()
    {
        var (servicio, repositorio) = CrearServicio();

        await servicio.CrearAsync(UsuarioId, "PUBLICACION", "Jornada publicada", "La jornada fue publicada.");

        Assert.Single(repositorio.Notificaciones);
        var notificacion = repositorio.Notificaciones[0];
        Assert.Equal(UsuarioId, notificacion.UsuarioId);
        Assert.Equal("PUBLICACION", notificacion.Tipo);
        Assert.False(notificacion.Leida);
    }

    [Fact]
    public async Task ObtenerPorUsuarioAsync_DevuelveSoloLasDelUsuarioIndicado_MasRecientesPrimero()
    {
        var (servicio, _) = CrearServicio();
        await servicio.CrearAsync(UsuarioId, "TIPO_A", "Primera", "Mensaje 1");
        await servicio.CrearAsync(UsuarioId, "TIPO_B", "Segunda", "Mensaje 2");
        await servicio.CrearAsync(2, "TIPO_C", "De otro usuario", "Mensaje 3");

        var resultado = await servicio.ObtenerPorUsuarioAsync(UsuarioId);

        Assert.Equal(2, resultado.Count);
        Assert.Equal("Segunda", resultado[0].Titulo);
        Assert.Equal("Primera", resultado[1].Titulo);
    }

    [Fact]
    public async Task MarcarComoLeidaAsync_MarcaLaNotificacionComoLeida()
    {
        var (servicio, repositorio) = CrearServicio();
        await servicio.CrearAsync(UsuarioId, "TIPO_A", "Título", "Mensaje");
        var notificacionId = repositorio.Notificaciones[0].NotificacionId;

        await servicio.MarcarComoLeidaAsync(UsuarioId, notificacionId);

        Assert.True(repositorio.Notificaciones[0].Leida);
    }

    [Fact]
    public async Task MarcarComoLeidaAsync_LanzaExcepcion_CuandoLaNotificacionNoExiste()
    {
        var (servicio, _) = CrearServicio();

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.MarcarComoLeidaAsync(UsuarioId, 999));
    }

    [Fact]
    public async Task MarcarComoLeidaAsync_LanzaExcepcion_CuandoLaNotificacionPerteneceAOtroUsuario()
    {
        var (servicio, repositorio) = CrearServicio();
        await servicio.CrearAsync(2, "TIPO_A", "Título", "Mensaje");
        var notificacionId = repositorio.Notificaciones[0].NotificacionId;

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.MarcarComoLeidaAsync(UsuarioId, notificacionId));
    }
}
