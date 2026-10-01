using TransportApp.Application.DTOs.Autenticacion;
using TransportApp.Application.Implementations;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Utils;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;

namespace TransportApp.UnitTests.Application.Implementations;

public class ServicioAutenticacionTests
{
    private static readonly IHasheadorContrasenas Hasheador = new HasheadorContrasenas();

    private class UsuarioRepositorioFalso : IUsuarioRepositorio
    {
        private readonly Usuario? _usuario;

        public UsuarioRepositorioFalso(Usuario? usuario) => _usuario = usuario;

        public Task<Usuario?> ObtenerPorCedulaConRolesAsync(string cedula) => Task.FromResult(_usuario);

        public Task<Usuario?> ObtenerPorIdentificadorConRolesAsync(string identificador) => Task.FromResult(_usuario);

        public Task<Usuario?> ObtenerPorIdAsync(int usuarioId) => Task.FromResult(_usuario);

        public Task<Usuario?> ObtenerPorEmailAsync(string email) => Task.FromResult(_usuario);

        public Task AgregarAsync(Usuario usuario) => Task.CompletedTask;

        public void Descartar(Usuario usuario) { }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class GeneradorTokenJwtFalso : IGeneradorTokenJwt
    {
        public (string Token, DateTime ExpiraEnUtc) GenerarToken(Usuario usuario, IEnumerable<UsuarioRol> rolesActivos)
            => ("token-de-prueba", DateTime.UtcNow.AddHours(1));
    }

    private static Usuario CrearUsuarioActivo(string password, params UsuarioRol[] roles)
    {
        var usuario = new Usuario
        {
            UsuarioId = 1,
            Cedula = "123456789",
            PasswordHash = Hasheador.Hashear(password),
            CorreoConfirmado = true,
            Activo = true
        };

        foreach (var rol in roles)
        {
            usuario.UsuarioRoles.Add(rol);
        }

        return usuario;
    }

    [Fact]
    public async Task IniciarSesionAsync_DevuelveToken_CuandoCredencialesSonValidas()
    {
        var usuario = CrearUsuarioActivo("Clave123", new UsuarioRol { Rol = Rol.EMPLEADO, EmpresaId = 1, Activo = true });
        var servicio = new ServicioAutenticacion(
            new UsuarioRepositorioFalso(usuario), Hasheador, new GeneradorTokenJwtFalso());

        var respuesta = await servicio.IniciarSesionAsync(new IniciarSesionDto { Identificador = "123456789", Password = "Clave123" });

        Assert.Equal("token-de-prueba", respuesta.Token);
    }

    [Fact]
    public async Task IniciarSesionAsync_LanzaExcepcion_CuandoElUsuarioNoExiste()
    {
        var servicio = new ServicioAutenticacion(
            new UsuarioRepositorioFalso(null), Hasheador, new GeneradorTokenJwtFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.IniciarSesionAsync(new IniciarSesionDto { Identificador = "000", Password = "x" }));
    }

    [Fact]
    public async Task IniciarSesionAsync_LanzaExcepcion_CuandoLaCuentaEstaInactiva()
    {
        var usuario = CrearUsuarioActivo("Clave123", new UsuarioRol { Rol = Rol.EMPLEADO, EmpresaId = 1, Activo = true });
        usuario.Activo = false;
        var servicio = new ServicioAutenticacion(
            new UsuarioRepositorioFalso(usuario), Hasheador, new GeneradorTokenJwtFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.IniciarSesionAsync(new IniciarSesionDto { Identificador = "123456789", Password = "Clave123" }));
    }

    [Fact]
    public async Task IniciarSesionAsync_LanzaExcepcion_CuandoLaCuentaEstaPendienteDeActivacion()
    {
        var usuario = new Usuario { UsuarioId = 1, Cedula = "123456789", PasswordHash = null, Activo = true };
        var servicio = new ServicioAutenticacion(
            new UsuarioRepositorioFalso(usuario), Hasheador, new GeneradorTokenJwtFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.IniciarSesionAsync(new IniciarSesionDto { Identificador = "123456789", Password = "Clave123" }));
    }

    [Fact]
    public async Task IniciarSesionAsync_LanzaExcepcion_CuandoElCorreoNoEstaConfirmado()
    {
        var usuario = CrearUsuarioActivo("Clave123", new UsuarioRol { Rol = Rol.EMPLEADO, EmpresaId = null, Activo = true });
        usuario.CorreoConfirmado = false;
        var servicio = new ServicioAutenticacion(
            new UsuarioRepositorioFalso(usuario), Hasheador, new GeneradorTokenJwtFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.IniciarSesionAsync(new IniciarSesionDto { Identificador = "123456789", Password = "Clave123" }));
    }

    [Fact]
    public async Task IniciarSesionAsync_PermiteEmpleadoSinEmpresa_CuandoElCorreoEstaConfirmado()
    {
        var usuario = CrearUsuarioActivo("Clave123", new UsuarioRol { Rol = Rol.EMPLEADO, EmpresaId = null, Activo = true });
        var servicio = new ServicioAutenticacion(
            new UsuarioRepositorioFalso(usuario), Hasheador, new GeneradorTokenJwtFalso());

        var respuesta = await servicio.IniciarSesionAsync(new IniciarSesionDto { Identificador = "123456789", Password = "Clave123" });

        Assert.Equal("token-de-prueba", respuesta.Token);
    }

    [Fact]
    public async Task IniciarSesionAsync_LanzaExcepcion_CuandoLaContrasenaEsIncorrecta()
    {
        var usuario = CrearUsuarioActivo("Clave123", new UsuarioRol { Rol = Rol.EMPLEADO, EmpresaId = 1, Activo = true });
        var servicio = new ServicioAutenticacion(
            new UsuarioRepositorioFalso(usuario), Hasheador, new GeneradorTokenJwtFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.IniciarSesionAsync(new IniciarSesionDto { Identificador = "123456789", Password = "Incorrecta" }));
    }

    [Fact]
    public async Task IniciarSesionAsync_LanzaExcepcion_CuandoNoTieneRolesActivos()
    {
        var usuario = CrearUsuarioActivo("Clave123", new UsuarioRol { Rol = Rol.EMPLEADO, EmpresaId = 1, Activo = false });
        var servicio = new ServicioAutenticacion(
            new UsuarioRepositorioFalso(usuario), Hasheador, new GeneradorTokenJwtFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.IniciarSesionAsync(new IniciarSesionDto { Identificador = "123456789", Password = "Clave123" }));
    }

    [Fact]
    public async Task IniciarSesionAsync_PermiteInicioDeSesion_CuandoUsuarioTieneMultiplesRoles()
    {
        var usuario = CrearUsuarioActivo(
            "Clave123",
            new UsuarioRol { Rol = Rol.EMPLEADO, EmpresaId = 1, Activo = true },
            new UsuarioRol { Rol = Rol.CONDUCTOR, EmpresaId = null, Activo = true },
            new UsuarioRol { Rol = Rol.COORDINADOR, EmpresaId = 1, Activo = true });

        var servicio = new ServicioAutenticacion(
            new UsuarioRepositorioFalso(usuario), Hasheador, new GeneradorTokenJwtFalso());

        var respuesta = await servicio.IniciarSesionAsync(new IniciarSesionDto { Identificador = "123456789", Password = "Clave123" });

        Assert.NotNull(respuesta.Token);
    }
}
