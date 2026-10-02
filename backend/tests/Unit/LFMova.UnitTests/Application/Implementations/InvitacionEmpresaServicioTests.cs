using LFMova.Application.DTOs.Autenticacion;
using LFMova.Application.DTOs.Invitaciones;
using LFMova.Application.DTOs.Notificaciones;
using LFMova.Application.DTOs.Personas;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

public class InvitacionEmpresaServicioTests
{
    private const int EmpresaId = 5;
    private const int CoordinadorId = 100;

    private class InvitacionRepositorioFalso : IInvitacionEmpresaRepositorio
    {
        public readonly List<InvitacionEmpresa> Invitaciones = new();
        private int _siguienteId = 1;

        public Task<InvitacionEmpresa?> ObtenerPorIdAsync(int invitacionEmpresaId)
            => Task.FromResult(Invitaciones.FirstOrDefault(i => i.InvitacionEmpresaId == invitacionEmpresaId));

        public Task<List<InvitacionEmpresa>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(Invitaciones.Where(i => i.EmpresaId == empresaId).ToList());

        public Task<List<InvitacionEmpresa>> ObtenerPorEmpresaYCedulaAsync(int empresaId, string cedula)
            => Task.FromResult(Invitaciones.Where(i => i.EmpresaId == empresaId && i.Cedula == cedula).ToList());

        public Task AgregarAsync(InvitacionEmpresa invitacion)
        {
            invitacion.InvitacionEmpresaId = _siguienteId++;
            Invitaciones.Add(invitacion);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class UsuarioRepositorioFalso : IUsuarioRepositorio
    {
        public readonly List<Usuario> Usuarios = new();

        public Task<Usuario?> ObtenerPorIdAsync(int usuarioId) => Task.FromResult(Usuarios.FirstOrDefault(u => u.UsuarioId == usuarioId));

        public Task<Usuario?> ObtenerPorCedulaConRolesAsync(string cedula) => Task.FromResult(Usuarios.FirstOrDefault(u => u.Cedula == cedula));

        public Task<Usuario?> ObtenerPorIdentificadorConRolesAsync(string identificador)
            => Task.FromResult(Usuarios.FirstOrDefault(u => u.Cedula == identificador || u.Email == identificador));

        public Task<Usuario?> ObtenerPorEmailAsync(string email) => Task.FromResult(Usuarios.FirstOrDefault(u => u.Email == email));

        public Task AgregarAsync(Usuario usuario)
        {
            Usuarios.Add(usuario);
            return Task.CompletedTask;
        }

        public void Descartar(Usuario usuario) { }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class UsuarioRolRepositorioFalso : IUsuarioRolRepositorio
    {
        public readonly List<UsuarioRol> Agregados = new();

        public Task<UsuarioRol?> ObtenerPorIdAsync(int usuarioRolId) => Task.FromResult<UsuarioRol?>(null);

        public Task<List<UsuarioRol>> ObtenerPorEmpresaYRolAsync(int empresaId, Rol rol) => Task.FromResult(new List<UsuarioRol>());

        public Task AgregarAsync(UsuarioRol usuarioRol)
        {
            Agregados.Add(usuarioRol);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class EmpleadoRepositorioFalso : IEmpleadoRepositorio
    {
        public readonly List<Empleado> Empleados = new();

        public Task<Empleado?> ObtenerPorIdAsync(int empleadoId) => Task.FromResult(Empleados.FirstOrDefault(e => e.EmpleadoId == empleadoId));

        public Task<Empleado?> ObtenerPorUsuarioIdAsync(int usuarioId) => Task.FromResult(Empleados.FirstOrDefault(e => e.UsuarioId == usuarioId));

        public Task<List<Empleado>> ObtenerPorEmpresaAsync(int empresaId) => Task.FromResult(Empleados.Where(e => e.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(Empleado empleado)
        {
            Empleados.Add(empleado);
            return Task.CompletedTask;
        }

        public void Descartar(Empleado empleado) { }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class EmpresaRepositorioFalso : IEmpresaRepositorio
    {
        private readonly Empresa _empresa = new() { EmpresaId = EmpresaId, Nombre = "Transportes Cinco" };

        public Task<Empresa?> ObtenerPorIdAsync(int empresaId) => Task.FromResult(empresaId == EmpresaId ? _empresa : null);

        public Task<Empresa?> ObtenerPorCifAsync(string cif) => Task.FromResult<Empresa?>(null);

        public Task<List<Empresa>> ObtenerTodasAsync() => Task.FromResult(new List<Empresa> { _empresa });

        public Task AgregarAsync(Empresa empresa) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    /// <summary>Solo se usa para saber si la persona ya forma parte de la empresa; la regla completa se prueba en PersonaServicioTests.</summary>
    private class PersonaServicioFalso : IPersonaServicio
    {
        public readonly HashSet<string> CedulasRelacionadas = new();

        public Task<PersonaDto> BuscarPorCedulaAsync(string cedula, int? empresaIdRestringida = null) => throw new NotSupportedException();

        public Task<bool> EstaRelacionadaConEmpresaAsync(string cedula, int empresaId) => Task.FromResult(CedulasRelacionadas.Contains(cedula));

        public Task<bool> PuedeGestionarseDesdeEmpresaAsync(string cedula, int empresaId) => throw new NotSupportedException();
    }

    private class NotificacionServicioFalso : INotificacionServicio
    {
        public readonly List<(int UsuarioId, string Tipo)> Creadas = new();

        public Task CrearAsync(int usuarioId, string tipo, string titulo, string mensaje, string? enlace = null)
        {
            Creadas.Add((usuarioId, tipo));
            return Task.CompletedTask;
        }

        public Task<List<NotificacionDto>> ObtenerPorUsuarioAsync(int usuarioId) => Task.FromResult(new List<NotificacionDto>());

        public Task MarcarComoLeidaAsync(int usuarioId, int notificacionId) => Task.CompletedTask;
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

    private sealed record Contexto(
        InvitacionEmpresaServicio Servicio,
        InvitacionRepositorioFalso Invitaciones,
        UsuarioRepositorioFalso Usuarios,
        EmpleadoRepositorioFalso Empleados,
        PersonaServicioFalso Personas,
        NotificacionServicioFalso Notificaciones,
        CorreoFalso Correo);

    private static Contexto Crear()
    {
        var invitaciones = new InvitacionRepositorioFalso();
        var usuarios = new UsuarioRepositorioFalso();
        usuarios.Usuarios.Add(new Usuario { UsuarioId = CoordinadorId, Cedula = "5000", NombreCompleto = "Carla Coordinadora", Email = "carla@test.com" });
        var empleados = new EmpleadoRepositorioFalso();
        var personas = new PersonaServicioFalso();
        var notificaciones = new NotificacionServicioFalso();
        var correo = new CorreoFalso();
        var servicio = InvitacionEmpresaServicioFabrica.Crear(
            invitaciones, usuarios, new UsuarioRolRepositorioFalso(), empleados, new EmpresaRepositorioFalso(), personas,
            notificaciones, new HasheadorContrasenas(), correo, new OpcionesFrontend { UrlBase = "http://front.test" });
        return new Contexto(servicio, invitaciones, usuarios, empleados, personas, notificaciones, correo);
    }

    private static Usuario AgregarUsuario(Contexto c, int usuarioId, string cedula, string? email)
    {
        var usuario = new Usuario { UsuarioId = usuarioId, Cedula = cedula, NombreCompleto = "Pedro Pérez", Email = email, Telefono = "3001112233", PasswordHash = "hash" };
        usuario.UsuarioRoles.Add(new UsuarioRol { UsuarioId = usuarioId, Rol = Rol.EMPLEADO, EmpresaId = null, Activo = true });
        c.Usuarios.Usuarios.Add(usuario);
        return usuario;
    }

    private static string ExtraerToken(string cuerpo)
    {
        var inicio = cuerpo.IndexOf("token=", StringComparison.Ordinal) + "token=".Length;
        var fin = cuerpo.IndexOf('"', inicio);
        return Uri.UnescapeDataString(cuerpo[inicio..fin]);
    }

    [Fact]
    public async Task InvitarAsync_EnviaAlCorreoDeLaCuenta_NoAlEscritoPorElCoordinador()
    {
        var c = Crear();
        AgregarUsuario(c, 1, "1001", "pedro.real@test.com");

        await c.Servicio.InvitarAsync(EmpresaId, new CrearInvitacionEmpresaDto { Cedula = "1001", Correo = "otro@test.com" }, CoordinadorId);

        var enviado = Assert.Single(c.Correo.Enviados);
        Assert.Equal("pedro.real@test.com", enviado.Destino);
        Assert.Contains("Transportes Cinco", enviado.Asunto);
        var invitacion = Assert.Single(c.Invitaciones.Invitaciones);
        Assert.NotEmpty(invitacion.TokenHash);
        Assert.Null(invitacion.FechaAceptacion);
    }

    [Fact]
    public async Task InvitarAsync_SinCuenta_EnviaAlCorreoEscrito()
    {
        var c = Crear();

        await c.Servicio.InvitarAsync(EmpresaId, new CrearInvitacionEmpresaDto { Cedula = "2002", Correo = "nuevo@test.com" }, CoordinadorId);

        Assert.Equal("nuevo@test.com", Assert.Single(c.Correo.Enviados).Destino);
    }

    [Fact]
    public async Task InvitarAsync_Falla_SiLaPersonaYaEsDeLaEmpresa()
    {
        var c = Crear();
        c.Personas.CedulasRelacionadas.Add("1001");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            c.Servicio.InvitarAsync(EmpresaId, new CrearInvitacionEmpresaDto { Cedula = "1001", Correo = "x@test.com" }, CoordinadorId));
        Assert.Empty(c.Correo.Enviados);
    }

    [Fact]
    public async Task InvitarAsync_Falla_SiElCorreoEscritoEsDeOtraCuenta()
    {
        var c = Crear();
        AgregarUsuario(c, 1, "1001", "pedro@test.com");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            c.Servicio.InvitarAsync(EmpresaId, new CrearInvitacionEmpresaDto { Cedula = "2002", Correo = "pedro@test.com" }, CoordinadorId));
    }

    [Fact]
    public async Task InvitarAsync_ReemplazaLaInvitacionPendienteAnterior()
    {
        var c = Crear();
        var datos = new CrearInvitacionEmpresaDto { Cedula = "2002", Correo = "nuevo@test.com" };

        await c.Servicio.InvitarAsync(EmpresaId, datos, CoordinadorId);
        var tokenViejo = ExtraerToken(c.Correo.Enviados[0].Cuerpo);
        await c.Servicio.InvitarAsync(EmpresaId, datos, CoordinadorId);

        var detalleViejo = await c.Servicio.ObtenerDetalleAsync(tokenViejo);
        Assert.Equal("VENCIDA", detalleViejo.Estado);
    }

    [Fact]
    public async Task AceptarAsync_CreaElEmpleadoDeLaEmpresaSinDireccion_YNotificaAlCoordinador()
    {
        var c = Crear();
        var usuario = AgregarUsuario(c, 1, "1001", "pedro@test.com");
        await c.Servicio.InvitarAsync(EmpresaId, new CrearInvitacionEmpresaDto { Cedula = "1001", Correo = "pedro@test.com" }, CoordinadorId);
        var token = ExtraerToken(c.Correo.Enviados[0].Cuerpo);

        await c.Servicio.AceptarAsync(token, usuario.UsuarioId);

        var empleado = Assert.Single(c.Empleados.Empleados);
        Assert.Equal(EmpresaId, empleado.EmpresaId);
        Assert.Equal(string.Empty, empleado.Direccion);
        Assert.Equal(EmpresaId, usuario.UsuarioRoles.Single(r => r.Rol == Rol.EMPLEADO).EmpresaId);
        Assert.NotNull(c.Invitaciones.Invitaciones[0].FechaAceptacion);
        Assert.Contains(c.Notificaciones.Creadas, n => n.UsuarioId == CoordinadorId && n.Tipo == "INVITACION_ACEPTADA");
    }

    [Fact]
    public async Task AceptarAsync_NoMueveAQuienYaEsEmpleadoDeOtraEmpresa()
    {
        var c = Crear();
        var usuario = AgregarUsuario(c, 1, "1001", "pedro@test.com");
        c.Empleados.Empleados.Add(new Empleado { EmpleadoId = 9, UsuarioId = 1, EmpresaId = 77, Direccion = "Calle 1" });
        await c.Servicio.InvitarAsync(EmpresaId, new CrearInvitacionEmpresaDto { Cedula = "1001", Correo = "pedro@test.com" }, CoordinadorId);

        await c.Servicio.AceptarAsync(ExtraerToken(c.Correo.Enviados[0].Cuerpo), usuario.UsuarioId);

        Assert.Equal(77, Assert.Single(c.Empleados.Empleados).EmpresaId);
        Assert.NotNull(c.Invitaciones.Invitaciones[0].FechaAceptacion);
    }

    [Fact]
    public async Task AceptarAsync_Falla_SiQuienAceptaTieneOtraCedula()
    {
        var c = Crear();
        AgregarUsuario(c, 1, "1001", "pedro@test.com");
        var intruso = AgregarUsuario(c, 2, "3003", "intruso@test.com");
        await c.Servicio.InvitarAsync(EmpresaId, new CrearInvitacionEmpresaDto { Cedula = "1001", Correo = "pedro@test.com" }, CoordinadorId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            c.Servicio.AceptarAsync(ExtraerToken(c.Correo.Enviados[0].Cuerpo), intruso.UsuarioId));
        Assert.Empty(c.Empleados.Empleados);
    }

    [Fact]
    public async Task AceptarAsync_Falla_ConUnTokenAlterado()
    {
        var c = Crear();
        var usuario = AgregarUsuario(c, 1, "1001", "pedro@test.com");
        await c.Servicio.InvitarAsync(EmpresaId, new CrearInvitacionEmpresaDto { Cedula = "1001", Correo = "pedro@test.com" }, CoordinadorId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Servicio.AceptarAsync("1.inventado", usuario.UsuarioId));
    }

    [Fact]
    public async Task AceptarAsync_Falla_SiLaInvitacionVencio()
    {
        var c = Crear();
        var usuario = AgregarUsuario(c, 1, "1001", "pedro@test.com");
        await c.Servicio.InvitarAsync(EmpresaId, new CrearInvitacionEmpresaDto { Cedula = "1001", Correo = "pedro@test.com" }, CoordinadorId);
        c.Invitaciones.Invitaciones[0].FechaExpiracion = DateTime.UtcNow.AddMinutes(-1);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            c.Servicio.AceptarAsync(ExtraerToken(c.Correo.Enviados[0].Cuerpo), usuario.UsuarioId));
    }

    [Fact]
    public async Task ObtenerDetalleAsync_IndicaQueRequiereRegistro_CuandoLaCedulaNoTieneCuenta()
    {
        var c = Crear();
        await c.Servicio.InvitarAsync(EmpresaId, new CrearInvitacionEmpresaDto { Cedula = "2002", Correo = "nuevo@test.com" }, CoordinadorId);

        var detalle = await c.Servicio.ObtenerDetalleAsync(ExtraerToken(c.Correo.Enviados[0].Cuerpo));

        Assert.True(detalle.RequiereRegistro);
        Assert.Equal("Transportes Cinco", detalle.NombreEmpresa);
        Assert.Equal("Carla Coordinadora", detalle.NombreInvitador);
        Assert.Equal("PENDIENTE", detalle.Estado);
    }

    [Fact]
    public async Task ObtenerPorEmpresaAsync_NoMuestraElNombreHastaQueSeAcepta()
    {
        var c = Crear();
        var usuario = AgregarUsuario(c, 1, "1001", "pedro@test.com");
        await c.Servicio.InvitarAsync(EmpresaId, new CrearInvitacionEmpresaDto { Cedula = "1001", Correo = "pedro@test.com" }, CoordinadorId);

        var antes = Assert.Single(await c.Servicio.ObtenerPorEmpresaAsync(EmpresaId));
        Assert.Null(antes.NombreCompleto);
        Assert.Equal("PENDIENTE", antes.Estado);

        await c.Servicio.AceptarAsync(ExtraerToken(c.Correo.Enviados[0].Cuerpo), usuario.UsuarioId);
        c.Invitaciones.Invitaciones[0].UsuarioAceptante = usuario;

        var despues = Assert.Single(await c.Servicio.ObtenerPorEmpresaAsync(EmpresaId));
        Assert.Equal("Pedro Pérez", despues.NombreCompleto);
        Assert.Equal("ACEPTADA", despues.Estado);
    }
}
