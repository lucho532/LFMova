using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Application.Utils;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Pruebas de las reglas de <see cref="EliminacionPersonaServicio"/>: quién
/// puede eliminarse y con qué alcance. El borrado real en la base se prueba
/// en las pruebas de integración.
/// </summary>
public class EliminacionPersonaServicioTests
{
    private const int EmpresaId = 1;
    private const int CoordinadorId = 900;
    private const string Clave = "ClaveSegura123";

    private class EmpleadoRepositorioFalso : IEmpleadoRepositorio
    {
        public readonly List<Empleado> Empleados = new();

        public Task<Empleado?> ObtenerPorIdAsync(int empleadoId) => Task.FromResult(Empleados.FirstOrDefault(e => e.EmpleadoId == empleadoId));
        public Task<Empleado?> ObtenerPorUsuarioIdAsync(int usuarioId) => Task.FromResult(Empleados.FirstOrDefault(e => e.UsuarioId == usuarioId));
        public Task<List<Empleado>> ObtenerPorEmpresaAsync(int empresaId) => Task.FromResult(Empleados.Where(e => e.EmpresaId == empresaId).ToList());
        public Task AgregarAsync(Empleado empleado) => throw new NotSupportedException();
        public void Descartar(Empleado empleado) => throw new NotSupportedException();
        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class UsuarioRepositorioFalso : IUsuarioRepositorio
    {
        public readonly List<Usuario> Usuarios = new();

        public Task<Usuario?> ObtenerPorIdAsync(int usuarioId) => Task.FromResult(Usuarios.FirstOrDefault(u => u.UsuarioId == usuarioId));
        public Task<Usuario?> ObtenerPorCedulaConRolesAsync(string cedula) => Task.FromResult(Usuarios.FirstOrDefault(u => u.Cedula == cedula));
        public Task<Usuario?> ObtenerPorIdentificadorConRolesAsync(string identificador) => throw new NotSupportedException();
        public Task<Usuario?> ObtenerPorEmailAsync(string email) => throw new NotSupportedException();
        public Task AgregarAsync(Usuario usuario) => throw new NotSupportedException();
        public void Descartar(Usuario usuario) => throw new NotSupportedException();
        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class EliminacionRepositorioFalso : IEliminacionPersonaRepositorio
    {
        public bool RutaEnCurso;
        public int OtrosAdministradores;
        public List<int> Empresas = new() { EmpresaId };
        public int? CuentaEliminada;
        public (int UsuarioId, int EmpresaId)? QuitadaDeEmpresa;

        public Task<bool> TieneRutaEnCursoAsync(int usuarioId) => Task.FromResult(RutaEnCurso);
        public Task<int> ContarOtrosAdministradoresAsync(int usuarioId) => Task.FromResult(OtrosAdministradores);
        public Task<List<int>> ObtenerEmpresasRelacionadasAsync(int usuarioId) => Task.FromResult(Empresas);

        public Task<List<string>> EliminarCuentaAsync(int usuarioId)
        {
            CuentaEliminada = usuarioId;
            return Task.FromResult(new List<string> { "evidencias/foto.jpg" });
        }

        public Task<List<string>> QuitarDeEmpresaAsync(int usuarioId, int empresaId)
        {
            QuitadaDeEmpresa = (usuarioId, empresaId);
            return Task.FromResult(new List<string>());
        }
    }

    private class AlmacenamientoFalso : IAlmacenamientoArchivos
    {
        public readonly List<string> Eliminados = new();

        public Task<string> GuardarAsync(string carpeta, string extension, Stream contenido) => throw new NotSupportedException();
        public Task<Stream?> AbrirAsync(string referencia) => throw new NotSupportedException();

        public Task EliminarAsync(string referencia)
        {
            Eliminados.Add(referencia);
            return Task.CompletedTask;
        }
    }

    private sealed record Contexto(EliminacionPersonaServicio Servicio, EliminacionRepositorioFalso Eliminacion, AlmacenamientoFalso Almacenamiento, Usuario Usuario);

    /// <summary>Arma el servicio con un empleado (id 10, usuario 100) de la empresa 1 y el rol indicado además de EMPLEADO.</summary>
    private static Contexto Crear(Rol? rolAdicional = null)
    {
        var usuario = new Usuario { UsuarioId = 100, Cedula = "1001", NombreCompleto = "Pedro Pérez", PasswordHash = new HasheadorContrasenas().Hashear(Clave) };
        usuario.UsuarioRoles.Add(new UsuarioRol { UsuarioId = 100, Rol = Rol.EMPLEADO, EmpresaId = EmpresaId, Activo = true });
        if (rolAdicional is not null)
        {
            usuario.UsuarioRoles.Add(new UsuarioRol { UsuarioId = 100, Rol = rolAdicional.Value, EmpresaId = EmpresaId, Activo = true });
        }

        var empleados = new EmpleadoRepositorioFalso();
        empleados.Empleados.Add(new Empleado { EmpleadoId = 10, UsuarioId = 100, EmpresaId = EmpresaId, Usuario = usuario });
        var usuarios = new UsuarioRepositorioFalso();
        usuarios.Usuarios.Add(usuario);
        var eliminacion = new EliminacionRepositorioFalso();
        var almacenamiento = new AlmacenamientoFalso();
        return new Contexto(new EliminacionPersonaServicio(empleados, usuarios, eliminacion, almacenamiento, new HasheadorContrasenas()), eliminacion, almacenamiento, usuario);
    }

    [Fact]
    public async Task EliminarAsync_BorraLaCuentaCompletaYSusArchivos_CuandoLaPersonaSoloEsDeEstaEmpresa()
    {
        var c = Crear(Rol.CONDUCTOR);

        var resultado = await c.Servicio.EliminarAsync(EmpresaId, 10, CoordinadorId);

        Assert.True(resultado.CuentaEliminada);
        Assert.Equal(100, c.Eliminacion.CuentaEliminada);
        Assert.Null(c.Eliminacion.QuitadaDeEmpresa);
        Assert.Equal("evidencias/foto.jpg", Assert.Single(c.Almacenamiento.Eliminados));
    }

    [Fact]
    public async Task EliminarAsync_SoloLaQuitaDeLaEmpresa_CuandoLaPersonaTambienEsDeOtra()
    {
        var c = Crear(Rol.CONDUCTOR);
        c.Eliminacion.Empresas = new List<int> { EmpresaId, 2 };

        var resultado = await c.Servicio.EliminarAsync(EmpresaId, 10, CoordinadorId);

        Assert.False(resultado.CuentaEliminada);
        Assert.Null(c.Eliminacion.CuentaEliminada);
        Assert.Equal((100, EmpresaId), c.Eliminacion.QuitadaDeEmpresa);
    }

    [Fact]
    public async Task EliminarAsync_LanzaExcepcion_CuandoElEmpleadoEsDeOtraEmpresa()
    {
        var c = Crear();

        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Servicio.EliminarAsync(empresaId: 2, 10, CoordinadorId));

        Assert.Null(c.Eliminacion.CuentaEliminada);
    }

    [Fact]
    public async Task EliminarAsync_LanzaExcepcion_CuandoElCoordinadorIntentaEliminarseASiMismo()
    {
        var c = Crear();

        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Servicio.EliminarAsync(EmpresaId, 10, usuarioSolicitanteId: 100));

        Assert.Null(c.Eliminacion.CuentaEliminada);
    }

    [Theory]
    [InlineData(Rol.COORDINADOR)]
    [InlineData(Rol.ADMINISTRADOR_PLATAFORMA)]
    public async Task EliminarAsync_LanzaExcepcion_CuandoLaPersonaEsCoordinadoraOAdministradora(Rol rol)
    {
        var c = Crear(rol);

        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Servicio.EliminarAsync(EmpresaId, 10, CoordinadorId));

        Assert.Null(c.Eliminacion.CuentaEliminada);
    }

    [Fact]
    public async Task EliminarAsync_LanzaExcepcion_CuandoLaPersonaTieneUnaRutaEnCurso()
    {
        var c = Crear();
        c.Eliminacion.RutaEnCurso = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Servicio.EliminarAsync(EmpresaId, 10, CoordinadorId));

        Assert.Null(c.Eliminacion.CuentaEliminada);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(Rol.CONDUCTOR)]
    [InlineData(Rol.COORDINADOR)]
    public async Task EliminarPropiaCuentaAsync_BorraLaCuentaCompleta_SeaCualSeaElRol(Rol? rol)
    {
        var c = Crear(rol);

        await c.Servicio.EliminarPropiaCuentaAsync(100, Clave);

        Assert.Equal(100, c.Eliminacion.CuentaEliminada);
        Assert.Single(c.Almacenamiento.Eliminados);
    }

    [Fact]
    public async Task EliminarPropiaCuentaAsync_LanzaExcepcion_CuandoLaContrasenaEsIncorrecta()
    {
        var c = Crear();

        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Servicio.EliminarPropiaCuentaAsync(100, "OtraClave123"));

        Assert.Null(c.Eliminacion.CuentaEliminada);
    }

    [Fact]
    public async Task EliminarPropiaCuentaAsync_LanzaExcepcion_CuandoTieneUnaRutaEnCurso()
    {
        var c = Crear(Rol.CONDUCTOR);
        c.Eliminacion.RutaEnCurso = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Servicio.EliminarPropiaCuentaAsync(100, Clave));

        Assert.Null(c.Eliminacion.CuentaEliminada);
    }

    [Fact]
    public async Task EliminarPropiaCuentaAsync_LanzaExcepcion_CuandoEsLaUnicaAdministradora()
    {
        var c = Crear(Rol.ADMINISTRADOR_PLATAFORMA);

        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Servicio.EliminarPropiaCuentaAsync(100, Clave));

        Assert.Null(c.Eliminacion.CuentaEliminada);
    }

    [Fact]
    public async Task EliminarPropiaCuentaAsync_PermiteALaAdministradora_CuandoHayOtra()
    {
        var c = Crear(Rol.ADMINISTRADOR_PLATAFORMA);
        c.Eliminacion.OtrosAdministradores = 1;

        await c.Servicio.EliminarPropiaCuentaAsync(100, Clave);

        Assert.Equal(100, c.Eliminacion.CuentaEliminada);
    }
}
