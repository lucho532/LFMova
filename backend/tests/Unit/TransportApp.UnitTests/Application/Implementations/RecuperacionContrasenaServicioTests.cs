using TransportApp.Application.DTOs.Autenticacion;
using TransportApp.Application.Implementations;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Utils;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;

namespace TransportApp.UnitTests.Application.Implementations;

public class RecuperacionContrasenaServicioTests
{
    private static readonly IHasheadorContrasenas Hasheador = new HasheadorContrasenas();

    private class UsuarioRepositorioFalso : IUsuarioRepositorio
    {
        private readonly List<Usuario> _usuarios;

        public UsuarioRepositorioFalso(params Usuario[] usuarios) => _usuarios = usuarios.ToList();

        public Task<Usuario?> ObtenerPorIdAsync(int usuarioId) => Task.FromResult(_usuarios.FirstOrDefault(u => u.UsuarioId == usuarioId));

        public Task<Usuario?> ObtenerPorCedulaConRolesAsync(string cedula) => Task.FromResult(_usuarios.FirstOrDefault(u => u.Cedula == cedula));

        public Task<Usuario?> ObtenerPorIdentificadorConRolesAsync(string identificador)
            => Task.FromResult(_usuarios.FirstOrDefault(u => u.Cedula == identificador || u.Email == identificador));

        public Task<Usuario?> ObtenerPorEmailAsync(string email) => Task.FromResult(_usuarios.FirstOrDefault(u => u.Email == email));

        public Task AgregarAsync(Usuario usuario) => Task.CompletedTask;

        public void Descartar(Usuario usuario) { }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class TokenRepositorioFalso : ITokenVerificacionRepositorio
    {
        public readonly List<TokenVerificacion> Tokens = new();

        public Task<List<TokenVerificacion>> ObtenerNoUtilizadosPorUsuarioAsync(int usuarioId, TipoTokenVerificacion tipo)
            => Task.FromResult(Tokens.Where(t => t.UsuarioId == usuarioId && t.Tipo == tipo && !t.Utilizado).ToList());

        public Task AgregarAsync(TokenVerificacion token)
        {
            Tokens.Add(token);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class CorreoFalso : IServicioCorreo
    {
        public readonly List<(string Destino, string Cuerpo)> Enviados = new();

        public Task EnviarAsync(string destinatarioEmail, string destinatarioNombre, string asunto, string cuerpoHtml)
        {
            Enviados.Add((destinatarioEmail, cuerpoHtml));
            return Task.CompletedTask;
        }
    }

    private static (RecuperacionContrasenaServicio Servicio, Usuario Usuario, TokenRepositorioFalso Tokens, CorreoFalso Correo) Crear()
    {
        var usuario = new Usuario
        {
            UsuarioId = 7,
            Cedula = "1001",
            Email = "ana@test.com",
            NombreCompleto = "Ana Gómez",
            PasswordHash = Hasheador.Hashear("ClaveVieja1"),
            CorreoConfirmado = false,
            Activo = true
        };
        var tokens = new TokenRepositorioFalso();
        var correo = new CorreoFalso();
        var servicio = new RecuperacionContrasenaServicio(
            new UsuarioRepositorioFalso(usuario), tokens, Hasheador, correo, new OpcionesFrontend { UrlBase = "http://front.test" });
        return (servicio, usuario, tokens, correo);
    }

    private static string ExtraerToken(string cuerpo)
    {
        var inicio = cuerpo.IndexOf("token=", StringComparison.Ordinal) + "token=".Length;
        var fin = cuerpo.IndexOf('"', inicio);
        return Uri.UnescapeDataString(cuerpo[inicio..fin]);
    }

    [Fact]
    public async Task SolicitarAsync_EnviaElEnlace_CuandoLaCuentaExiste()
    {
        var (servicio, _, tokens, correo) = Crear();

        await servicio.SolicitarAsync(new SolicitarRecuperacionDto { Identificador = "ana@test.com" });

        Assert.Single(tokens.Tokens);
        var mensaje = Assert.Single(correo.Enviados);
        Assert.Contains("http://front.test/restablecer-contrasena?token=", mensaje.Cuerpo);
    }

    [Fact]
    public async Task SolicitarAsync_NoEnviaNada_NiFalla_CuandoLaCuentaNoExiste()
    {
        var (servicio, _, tokens, correo) = Crear();

        await servicio.SolicitarAsync(new SolicitarRecuperacionDto { Identificador = "no-existe" });

        Assert.Empty(tokens.Tokens);
        Assert.Empty(correo.Enviados);
    }

    [Fact]
    public async Task RestablecerAsync_CambiaLaContrasenaYConfirmaElCorreo()
    {
        var (servicio, usuario, _, correo) = Crear();
        await servicio.SolicitarAsync(new SolicitarRecuperacionDto { Identificador = "1001" });
        var token = ExtraerToken(correo.Enviados[0].Cuerpo);

        await servicio.RestablecerAsync(new RestablecerContrasenaDto { Token = token, NuevaContrasena = "Nueva123", ConfirmacionContrasena = "Nueva123" });

        Assert.True(Hasheador.Verificar("Nueva123", usuario.PasswordHash!));
        Assert.True(usuario.CorreoConfirmado);
    }

    [Fact]
    public async Task RestablecerAsync_NoPermiteReutilizarElToken()
    {
        var (servicio, _, _, correo) = Crear();
        await servicio.SolicitarAsync(new SolicitarRecuperacionDto { Identificador = "1001" });
        var token = ExtraerToken(correo.Enviados[0].Cuerpo);
        var datos = new RestablecerContrasenaDto { Token = token, NuevaContrasena = "Nueva123", ConfirmacionContrasena = "Nueva123" };
        await servicio.RestablecerAsync(datos);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.RestablecerAsync(datos));
    }

    [Fact]
    public async Task RestablecerAsync_LanzaExcepcion_CuandoLasContrasenasNoCoinciden()
    {
        var (servicio, _, _, correo) = Crear();
        await servicio.SolicitarAsync(new SolicitarRecuperacionDto { Identificador = "1001" });
        var token = ExtraerToken(correo.Enviados[0].Cuerpo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.RestablecerAsync(new RestablecerContrasenaDto { Token = token, NuevaContrasena = "A", ConfirmacionContrasena = "B" }));
    }

    [Fact]
    public async Task EnviarInvitacionAsync_EnviaUnEnlaceParaEstablecerContrasena()
    {
        var (servicio, usuario, tokens, correo) = Crear();

        await servicio.EnviarInvitacionAsync(usuario, "como coordinador de ACME");

        Assert.Single(tokens.Tokens);
        var mensaje = Assert.Single(correo.Enviados);
        Assert.Contains("como coordinador de ACME", mensaje.Cuerpo);
    }
}
