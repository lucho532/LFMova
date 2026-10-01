using LFMova.Application.DTOs.Empresas;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

public class AsignacionCoordinadorServicioTests
{
    private class EmpresaRepositorioFalso : IEmpresaRepositorio
    {
        private readonly Dictionary<int, Empresa> _empresas;

        public EmpresaRepositorioFalso(params Empresa[] empresas)
            => _empresas = empresas.ToDictionary(e => e.EmpresaId);

        public Task<Empresa?> ObtenerPorIdAsync(int empresaId)
            => Task.FromResult(_empresas.TryGetValue(empresaId, out var e) ? e : null);

        public Task<Empresa?> ObtenerPorCifAsync(string cif)
            => Task.FromResult(_empresas.Values.FirstOrDefault(e => e.Cif == cif));

        public Task<List<Empresa>> ObtenerTodasAsync() => Task.FromResult(_empresas.Values.ToList());

        public Task AgregarAsync(Empresa empresa) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class UsuarioRepositorioFalso : IUsuarioRepositorio
    {
        private readonly Dictionary<string, Usuario> _usuariosPorCedula;
        private int _siguienteId = 100;

        public UsuarioRepositorioFalso(params Usuario[] usuarios)
            => _usuariosPorCedula = usuarios.ToDictionary(u => u.Cedula);

        public Task<Usuario?> ObtenerPorCedulaConRolesAsync(string cedula)
            => Task.FromResult(_usuariosPorCedula.TryGetValue(cedula, out var u) ? u : null);

        public Task<Usuario?> ObtenerPorIdAsync(int usuarioId)
            => Task.FromResult(_usuariosPorCedula.Values.FirstOrDefault(u => u.UsuarioId == usuarioId));

        public Task<Usuario?> ObtenerPorIdentificadorConRolesAsync(string identificador)
            => Task.FromResult(_usuariosPorCedula.Values.FirstOrDefault(u => u.Cedula == identificador || u.Email == identificador));

        public Task<Usuario?> ObtenerPorEmailAsync(string email)
            => Task.FromResult(_usuariosPorCedula.Values.FirstOrDefault(u => u.Email == email));

        public Task AgregarAsync(Usuario usuario)
        {
            usuario.UsuarioId = _siguienteId++;
            _usuariosPorCedula[usuario.Cedula] = usuario;
            return Task.CompletedTask;
        }

        public void Descartar(Usuario usuario) { }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class UsuarioRolRepositorioFalso : IUsuarioRolRepositorio
    {
        public readonly List<UsuarioRol> Roles = new();
        private int _siguienteId = 1;

        public Task<UsuarioRol?> ObtenerPorIdAsync(int usuarioRolId)
            => Task.FromResult(Roles.FirstOrDefault(r => r.UsuarioRolId == usuarioRolId));

        public Task<List<UsuarioRol>> ObtenerPorEmpresaYRolAsync(int empresaId, Rol rol)
            => Task.FromResult(Roles.Where(r => r.EmpresaId == empresaId && r.Rol == rol).ToList());

        public Task AgregarAsync(UsuarioRol usuarioRol)
        {
            usuarioRol.UsuarioRolId = _siguienteId++;
            Roles.Add(usuarioRol);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class RecuperacionContrasenaFalsa : IRecuperacionContrasenaServicio
    {
        public readonly List<Usuario> Invitados = new();

        public Task SolicitarAsync(LFMova.Application.DTOs.Autenticacion.SolicitarRecuperacionDto datos) => Task.CompletedTask;

        public Task RestablecerAsync(LFMova.Application.DTOs.Autenticacion.RestablecerContrasenaDto datos) => Task.CompletedTask;

        public Task EnviarInvitacionAsync(Usuario usuarioInvitado, string contextoInvitacion)
        {
            Invitados.Add(usuarioInvitado);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task AsignarAsync_LanzaExcepcion_CuandoLaCuentaNoExisteYFaltanLosDatosDeContacto()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var servicio = new AsignacionCoordinadorServicio(new UsuarioRepositorioFalso(), new UsuarioRolRepositorioFalso(), empresaRepo, new RecuperacionContrasenaFalsa());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.AsignarAsync(1, new AsignarCoordinadorDto { Cedula = "999" }, usuarioEjecutorId: 1));
    }

    [Fact]
    public async Task AsignarAsync_CreaUsuarioYRol_CuandoLaCedulaNoExistiaTodavia()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var usuarioRepo = new UsuarioRepositorioFalso();
        var rolRepo = new UsuarioRolRepositorioFalso();
        var recuperacion = new RecuperacionContrasenaFalsa();
        var servicio = new AsignacionCoordinadorServicio(usuarioRepo, rolRepo, empresaRepo, recuperacion);

        await servicio.AsignarAsync(1, new AsignarCoordinadorDto
        {
            Cedula = "999",
            NombreCoordinador = "Nuevo Coordinador",
            TelefonoCoordinador = "3001112233",
            CorreoCoordinador = "nuevo@acme.test"
        }, usuarioEjecutorId: 1);

        var creado = await usuarioRepo.ObtenerPorCedulaConRolesAsync("999");
        Assert.NotNull(creado);
        Assert.Null(creado!.PasswordHash);
        Assert.Equal("nuevo@acme.test", creado.Email);
        Assert.Single(recuperacion.Invitados);
        Assert.Single(rolRepo.Roles);
        Assert.Equal(Rol.COORDINADOR, rolRepo.Roles[0].Rol);
        Assert.Equal(1, rolRepo.Roles[0].EmpresaId);
    }

    [Fact]
    public async Task AsignarAsync_LanzaExcepcion_CuandoEsAutoasignacion()
    {
        var usuarioExistente = new Usuario { UsuarioId = 5, Cedula = "111", Activo = true };
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var usuarioRepo = new UsuarioRepositorioFalso(usuarioExistente);
        var servicio = new AsignacionCoordinadorServicio(usuarioRepo, new UsuarioRolRepositorioFalso(), empresaRepo, new RecuperacionContrasenaFalsa());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.AsignarAsync(1, new AsignarCoordinadorDto { Cedula = "111" }, usuarioEjecutorId: 5));
    }

    [Fact]
    public async Task AsignarAsync_LanzaExcepcion_CuandoYaEsCoordinadorDeOtraEmpresa()
    {
        var usuarioExistente = new Usuario { UsuarioId = 5, Cedula = "111", Activo = true };
        usuarioExistente.UsuarioRoles.Add(new UsuarioRol { UsuarioRolId = 1, Rol = Rol.COORDINADOR, EmpresaId = 2, Activo = true });

        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var usuarioRepo = new UsuarioRepositorioFalso(usuarioExistente);
        var servicio = new AsignacionCoordinadorServicio(usuarioRepo, new UsuarioRolRepositorioFalso(), empresaRepo, new RecuperacionContrasenaFalsa());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.AsignarAsync(1, new AsignarCoordinadorDto { Cedula = "111" }, usuarioEjecutorId: 999));
    }

    [Fact]
    public async Task RevocarAsync_LanzaExcepcion_CuandoEsElUltimoCoordinadorActivo()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var rolRepo = new UsuarioRolRepositorioFalso();
        rolRepo.Roles.Add(new UsuarioRol { UsuarioRolId = 1, Rol = Rol.COORDINADOR, EmpresaId = 1, Activo = true });
        var servicio = new AsignacionCoordinadorServicio(new UsuarioRepositorioFalso(), rolRepo, empresaRepo, new RecuperacionContrasenaFalsa());

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.RevocarAsync(1, 1));
    }

    [Fact]
    public async Task RevocarAsync_Revoca_CuandoHayOtroCoordinadorActivo()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var rolRepo = new UsuarioRolRepositorioFalso();
        rolRepo.Roles.Add(new UsuarioRol { UsuarioRolId = 1, Rol = Rol.COORDINADOR, EmpresaId = 1, Activo = true });
        rolRepo.Roles.Add(new UsuarioRol { UsuarioRolId = 2, Rol = Rol.COORDINADOR, EmpresaId = 1, Activo = true });
        var servicio = new AsignacionCoordinadorServicio(new UsuarioRepositorioFalso(), rolRepo, empresaRepo, new RecuperacionContrasenaFalsa());

        await servicio.RevocarAsync(1, 1);

        Assert.False(rolRepo.Roles.First(r => r.UsuarioRolId == 1).Activo);
    }

    [Fact]
    public async Task ObtenerPorEmpresaAsync_DevuelveCoordinadoresActivosEInactivos()
    {
        var usuario = new Usuario { UsuarioId = 5, Cedula = "111", Activo = true };
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var usuarioRepo = new UsuarioRepositorioFalso(usuario);
        var rolRepo = new UsuarioRolRepositorioFalso();
        rolRepo.Roles.Add(new UsuarioRol { UsuarioRolId = 1, UsuarioId = 5, Usuario = usuario, Rol = Rol.COORDINADOR, EmpresaId = 1, Activo = true });
        rolRepo.Roles.Add(new UsuarioRol { UsuarioRolId = 2, UsuarioId = 6, Rol = Rol.COORDINADOR, EmpresaId = 1, Activo = false });
        var servicio = new AsignacionCoordinadorServicio(usuarioRepo, rolRepo, empresaRepo, new RecuperacionContrasenaFalsa());

        var coordinadores = await servicio.ObtenerPorEmpresaAsync(1);

        Assert.Equal(2, coordinadores.Count);
        Assert.Contains(coordinadores, c => c.UsuarioRolId == 1 && c.Cedula == "111" && c.Activo);
        Assert.Contains(coordinadores, c => c.UsuarioRolId == 2 && !c.Activo);
    }

    [Fact]
    public async Task ReactivarAsync_ReactivaElRol()
    {
        var usuario = new Usuario { UsuarioId = 5, Cedula = "111", Activo = true };
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var usuarioRepo = new UsuarioRepositorioFalso(usuario);
        var rolRepo = new UsuarioRolRepositorioFalso();
        rolRepo.Roles.Add(new UsuarioRol { UsuarioRolId = 1, UsuarioId = 5, Usuario = usuario, Rol = Rol.COORDINADOR, EmpresaId = 1, Activo = false });
        var servicio = new AsignacionCoordinadorServicio(usuarioRepo, rolRepo, empresaRepo, new RecuperacionContrasenaFalsa());

        await servicio.ReactivarAsync(1, 1);

        Assert.True(rolRepo.Roles.First(r => r.UsuarioRolId == 1).Activo);
    }

    [Fact]
    public async Task ReactivarAsync_LanzaExcepcion_CuandoYaEsCoordinadorActivoDeOtraEmpresa()
    {
        var usuario = new Usuario { UsuarioId = 5, Cedula = "111", Activo = true };
        var empresaRepo = new EmpresaRepositorioFalso(
            new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true },
            new Empresa { EmpresaId = 2, Nombre = "Otra", Activa = true });
        var usuarioRepo = new UsuarioRepositorioFalso(usuario);
        var rolRepo = new UsuarioRolRepositorioFalso();
        rolRepo.Roles.Add(new UsuarioRol { UsuarioRolId = 1, UsuarioId = 5, Usuario = usuario, Rol = Rol.COORDINADOR, EmpresaId = 1, Activo = false });
        rolRepo.Roles.Add(new UsuarioRol { UsuarioRolId = 2, UsuarioId = 5, Usuario = usuario, Rol = Rol.COORDINADOR, EmpresaId = 2, Activo = true });
        usuario.UsuarioRoles.Add(rolRepo.Roles[1]);
        var servicio = new AsignacionCoordinadorServicio(usuarioRepo, rolRepo, empresaRepo, new RecuperacionContrasenaFalsa());

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ReactivarAsync(1, 1));
    }

    [Fact]
    public async Task AsignarAsync_LanzaExcepcion_CuandoLaCedulaEstaVacia()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var servicio = new AsignacionCoordinadorServicio(new UsuarioRepositorioFalso(), new UsuarioRolRepositorioFalso(), empresaRepo, new RecuperacionContrasenaFalsa());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.AsignarAsync(1, new AsignarCoordinadorDto { Cedula = "" }, usuarioEjecutorId: 1));
    }
}
