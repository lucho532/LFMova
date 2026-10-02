using LFMova.Application.DTOs.Autenticacion;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

public class RegistroServicioTests
{
    private class UsuarioRepositorioFalso : IUsuarioRepositorio
    {
        public readonly List<Usuario> Usuarios = new();
        private int _siguienteId = 1;

        public Task<Usuario?> ObtenerPorIdAsync(int usuarioId) => Task.FromResult(Usuarios.FirstOrDefault(u => u.UsuarioId == usuarioId));

        public Task<Usuario?> ObtenerPorCedulaConRolesAsync(string cedula) => Task.FromResult(Usuarios.FirstOrDefault(u => u.Cedula == cedula));

        public Task<Usuario?> ObtenerPorIdentificadorConRolesAsync(string identificador)
            => Task.FromResult(Usuarios.FirstOrDefault(u => u.Cedula == identificador || u.Email == identificador));

        public Task<Usuario?> ObtenerPorEmailAsync(string email) => Task.FromResult(Usuarios.FirstOrDefault(u => u.Email == email));

        public Task AgregarAsync(Usuario usuario)
        {
            usuario.UsuarioId = _siguienteId++;
            Usuarios.Add(usuario);
            return Task.CompletedTask;
        }

        public void Descartar(Usuario usuario) { }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class UsuarioRolRepositorioFalso : IUsuarioRolRepositorio
    {
        public readonly List<UsuarioRol> Roles = new();

        public Task<UsuarioRol?> ObtenerPorIdAsync(int usuarioRolId) => Task.FromResult(Roles.FirstOrDefault(r => r.UsuarioRolId == usuarioRolId));

        public Task<List<UsuarioRol>> ObtenerPorEmpresaYRolAsync(int empresaId, Rol rol)
            => Task.FromResult(Roles.Where(r => r.EmpresaId == empresaId && r.Rol == rol).ToList());

        public Task AgregarAsync(UsuarioRol usuarioRol)
        {
            Roles.Add(usuarioRol);
            return Task.CompletedTask;
        }

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
        public readonly List<(string Destino, string Asunto, string Cuerpo)> Enviados = new();

        public Task EnviarConAdjuntoAsync(string destinatarioEmail, string destinatarioNombre, string asunto, string cuerpoHtml, AdjuntoCorreo adjunto)
            => EnviarAsync(destinatarioEmail, destinatarioNombre, asunto, cuerpoHtml);

        public Task EnviarAsync(string destinatarioEmail, string destinatarioNombre, string asunto, string cuerpoHtml)
        {
            Enviados.Add((destinatarioEmail, asunto, cuerpoHtml));
            return Task.CompletedTask;
        }
    }

    private static (RegistroServicio Servicio, UsuarioRepositorioFalso Usuarios, UsuarioRolRepositorioFalso Roles, TokenRepositorioFalso Tokens, CorreoFalso Correo) Crear()
    {
        var usuarios = new UsuarioRepositorioFalso();
        var roles = new UsuarioRolRepositorioFalso();
        var tokens = new TokenRepositorioFalso();
        var correo = new CorreoFalso();
        var servicio = new RegistroServicio(
            usuarios, roles, tokens, new HasheadorContrasenas(), correo,
            new OpcionesFrontend { UrlBase = "http://front.test" },
            new InvitacionServicioNoUsado());
        return (servicio, usuarios, roles, tokens, correo);
    }

    /// <summary>Estas pruebas registran sin token de invitación: el servicio de invitaciones nunca debe usarse.</summary>
    private class InvitacionServicioNoUsado : IInvitacionEmpresaServicio
    {
        public Task InvitarAsync(int empresaId, LFMova.Application.DTOs.Invitaciones.CrearInvitacionEmpresaDto datos, int usuarioInvitadorId) => throw new NotSupportedException();
        public Task<List<LFMova.Application.DTOs.Invitaciones.InvitacionEmpresaDto>> ObtenerPorEmpresaAsync(int empresaId) => throw new NotSupportedException();
        public Task<LFMova.Application.DTOs.Invitaciones.DetalleInvitacionDto> ObtenerDetalleAsync(string token) => throw new NotSupportedException();
        public Task AceptarAsync(string token, int usuarioId) => throw new NotSupportedException();
        public Task EliminarAsync(int empresaId, int invitacionEmpresaId) => throw new NotSupportedException();
        public Task<InvitacionEmpresa> ObtenerParaRegistroAsync(string token, string cedula) => throw new NotSupportedException();
        public Task AceptarParaUsuarioAsync(InvitacionEmpresa invitacion, Usuario usuario) => throw new NotSupportedException();
    }

    private static RegistrarUsuarioDto DatosValidos() => new()
    {
        Correo = "ana@test.com",
        NombreCompleto = "Ana Gómez",
        Cedula = "1001",
        Telefono = "3001234567",
        Password = "Clave123"
    };

    private static string ExtraerToken(string cuerpo)
    {
        var inicio = cuerpo.IndexOf("token=", StringComparison.Ordinal) + "token=".Length;
        var fin = cuerpo.IndexOf('"', inicio);
        return Uri.UnescapeDataString(cuerpo[inicio..fin]);
    }

    [Fact]
    public async Task RegistrarAsync_GuardaElCorreoEnMinusculasYSinEspacios()
    {
        var (servicio, usuarios, _, _, correo) = Crear();
        var datos = DatosValidos();
        datos.Correo = "  Ana@Test.COM ";

        await servicio.RegistrarAsync(datos);

        Assert.Equal("ana@test.com", Assert.Single(usuarios.Usuarios).Email);
        Assert.Equal("ana@test.com", Assert.Single(correo.Enviados).Destino);
    }

    [Fact]
    public async Task RegistrarAsync_LanzaExcepcion_CuandoElCorreoYaExisteConOtrasMayusculas()
    {
        var (servicio, _, _, _, _) = Crear();
        await servicio.RegistrarAsync(DatosValidos());

        var otro = DatosValidos();
        otro.Cedula = "2002";
        otro.Correo = "ANA@test.com";

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.RegistrarAsync(otro));
    }

    [Fact]
    public async Task RegistrarAsync_CreaCuentaEmpleadoSinEmpresa_YEnviaCorreoDeConfirmacion()
    {
        var (servicio, usuarios, roles, tokens, correo) = Crear();

        await servicio.RegistrarAsync(DatosValidos());

        var usuario = Assert.Single(usuarios.Usuarios);
        Assert.NotNull(usuario.PasswordHash);
        Assert.False(usuario.CorreoConfirmado);
        var rol = Assert.Single(roles.Roles);
        Assert.Equal(Rol.EMPLEADO, rol.Rol);
        Assert.Null(rol.EmpresaId);
        Assert.Single(tokens.Tokens);
        var mensaje = Assert.Single(correo.Enviados);
        Assert.Equal("ana@test.com", mensaje.Destino);
        Assert.Contains("http://front.test/confirmar-correo?token=", mensaje.Cuerpo);
    }

    [Fact]
    public async Task RegistrarAsync_LanzaExcepcion_CuandoLaCedulaYaExiste()
    {
        var (servicio, _, _, _, _) = Crear();
        await servicio.RegistrarAsync(DatosValidos());
        var otro = DatosValidos();
        otro.Correo = "otro@test.com";

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.RegistrarAsync(otro));
    }

    [Fact]
    public async Task RegistrarAsync_LanzaExcepcion_CuandoElCorreoYaExiste()
    {
        var (servicio, _, _, _, _) = Crear();
        await servicio.RegistrarAsync(DatosValidos());
        var otro = DatosValidos();
        otro.Cedula = "2002";

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.RegistrarAsync(otro));
    }

    [Fact]
    public async Task RegistrarAsync_LanzaExcepcion_CuandoElCorreoNoEsValido()
    {
        var (servicio, _, _, _, _) = Crear();
        var datos = DatosValidos();
        datos.Correo = "sin-arroba";

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.RegistrarAsync(datos));
    }

    [Fact]
    public async Task ConfirmarCorreoAsync_MarcaElCorreoComoConfirmado_ConElTokenEnviado()
    {
        var (servicio, usuarios, _, _, correo) = Crear();
        await servicio.RegistrarAsync(DatosValidos());
        var token = ExtraerToken(correo.Enviados[0].Cuerpo);

        await servicio.ConfirmarCorreoAsync(new ConfirmarCorreoDto { Token = token });

        Assert.True(usuarios.Usuarios[0].CorreoConfirmado);
    }

    [Fact]
    public async Task ConfirmarCorreoAsync_LanzaExcepcion_CuandoElTokenNoCoincide()
    {
        var (servicio, usuarios, _, _, _) = Crear();
        await servicio.RegistrarAsync(DatosValidos());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.ConfirmarCorreoAsync(new ConfirmarCorreoDto { Token = $"{usuarios.Usuarios[0].UsuarioId}.token-falso" }));
    }

    [Fact]
    public async Task ConfirmarCorreoAsync_LanzaExcepcion_CuandoElTokenExpiro()
    {
        var (servicio, _, _, tokens, correo) = Crear();
        await servicio.RegistrarAsync(DatosValidos());
        tokens.Tokens[0].FechaExpiracion = DateTime.UtcNow.AddMinutes(-1);
        var token = ExtraerToken(correo.Enviados[0].Cuerpo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ConfirmarCorreoAsync(new ConfirmarCorreoDto { Token = token }));
    }
}
