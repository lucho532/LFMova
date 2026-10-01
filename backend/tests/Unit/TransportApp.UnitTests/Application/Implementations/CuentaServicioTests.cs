using TransportApp.Application.DTOs.Cuenta;
using TransportApp.Application.Implementations;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Utils;
using TransportApp.Domain.Entities;

namespace TransportApp.UnitTests.Application.Implementations;

public class CuentaServicioTests
{
    private static readonly IHasheadorContrasenas Hasheador = new HasheadorContrasenas();

    private class UsuarioRepositorioFalso : IUsuarioRepositorio
    {
        private readonly Usuario? _usuario;

        public UsuarioRepositorioFalso(Usuario? usuario) => _usuario = usuario;

        public Task<Usuario?> ObtenerPorIdAsync(int usuarioId) => Task.FromResult(_usuario);

        public Task<Usuario?> ObtenerPorCedulaConRolesAsync(string cedula) => Task.FromResult(_usuario);

        public Task<Usuario?> ObtenerPorIdentificadorConRolesAsync(string identificador) => Task.FromResult(_usuario);

        public Task<Usuario?> ObtenerPorEmailAsync(string email) => Task.FromResult(_usuario);

        public Task AgregarAsync(Usuario usuario) => Task.CompletedTask;

        public void Descartar(Usuario usuario) { }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private static (CuentaServicio Servicio, Usuario Usuario) Crear()
    {
        var usuario = new Usuario
        {
            UsuarioId = 1,
            Cedula = "1001",
            Email = "ana@test.com",
            NombreCompleto = "Ana Gómez",
            Telefono = "3000000000",
            PasswordHash = Hasheador.Hashear("ClaveActual1"),
            Activo = true
        };
        return (new CuentaServicio(new UsuarioRepositorioFalso(usuario), Hasheador), usuario);
    }

    [Fact]
    public async Task ObtenerAsync_DevuelveLosDatosDeLaCuenta()
    {
        var (servicio, _) = Crear();

        var cuenta = await servicio.ObtenerAsync(1);

        Assert.Equal("1001", cuenta.Cedula);
        Assert.Equal("Ana Gómez", cuenta.NombreCompleto);
        Assert.Equal("ana@test.com", cuenta.Email);
    }

    [Fact]
    public async Task ActualizarAsync_CambiaNombreYTelefono_SinTocarCedulaNiCorreo()
    {
        var (servicio, usuario) = Crear();

        var cuenta = await servicio.ActualizarAsync(1, new ActualizarCuentaDto { NombreCompleto = "  Ana María Gómez ", Telefono = "3111111111" });

        Assert.Equal("Ana María Gómez", cuenta.NombreCompleto);
        Assert.Equal("3111111111", usuario.Telefono);
        Assert.Equal("1001", usuario.Cedula);
        Assert.Equal("ana@test.com", usuario.Email);
    }

    [Fact]
    public async Task ActualizarAsync_LanzaExcepcion_CuandoElNombreEstaVacio()
    {
        var (servicio, _) = Crear();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.ActualizarAsync(1, new ActualizarCuentaDto { NombreCompleto = " ", Telefono = "3111111111" }));
    }

    [Fact]
    public async Task CambiarContrasenaAsync_GuardaLaNuevaContrasena_CuandoLaActualEsCorrecta()
    {
        var (servicio, usuario) = Crear();

        await servicio.CambiarContrasenaAsync(1, new CambiarContrasenaDto { ContrasenaActual = "ClaveActual1", NuevaContrasena = "Nueva123", ConfirmacionContrasena = "Nueva123" });

        Assert.True(Hasheador.Verificar("Nueva123", usuario.PasswordHash!));
    }

    [Fact]
    public async Task CambiarContrasenaAsync_LanzaExcepcion_CuandoLaActualEsIncorrecta()
    {
        var (servicio, _) = Crear();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CambiarContrasenaAsync(1, new CambiarContrasenaDto { ContrasenaActual = "otra", NuevaContrasena = "Nueva123", ConfirmacionContrasena = "Nueva123" }));
    }

    [Fact]
    public async Task CambiarContrasenaAsync_LanzaExcepcion_CuandoNoCoincideLaConfirmacion()
    {
        var (servicio, _) = Crear();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CambiarContrasenaAsync(1, new CambiarContrasenaDto { ContrasenaActual = "ClaveActual1", NuevaContrasena = "A", ConfirmacionContrasena = "B" }));
    }

    [Fact]
    public async Task CambiarContrasenaAsync_LanzaExcepcion_CuandoLaNuevaEsIgualALaActual()
    {
        var (servicio, _) = Crear();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CambiarContrasenaAsync(1, new CambiarContrasenaDto { ContrasenaActual = "ClaveActual1", NuevaContrasena = "ClaveActual1", ConfirmacionContrasena = "ClaveActual1" }));
    }
}
