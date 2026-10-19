using LFMova.Application.DTOs.Empresas;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

public class EmpresaServicioTests
{
    private class EmpresaRepositorioFalso : IEmpresaRepositorio
    {
        private readonly Dictionary<int, Empresa> _empresas = new();
        private int _siguienteId = 1;

        public Task<Empresa?> ObtenerPorIdAsync(int empresaId)
            => Task.FromResult(_empresas.TryGetValue(empresaId, out var empresa) ? empresa : null);

        public Task<Empresa?> ObtenerPorCifAsync(string cif)
            => Task.FromResult(_empresas.Values.FirstOrDefault(e => e.Cif == cif));

        public Task<List<Empresa>> ObtenerTodasAsync() => Task.FromResult(_empresas.Values.ToList());

        public Task AgregarAsync(Empresa empresa)
        {
            empresa.EmpresaId = _siguienteId++;
            _empresas[empresa.EmpresaId] = empresa;
            return Task.CompletedTask;
        }

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
        public readonly List<(Usuario Usuario, string Contexto)> Invitaciones = new();

        public Task SolicitarAsync(LFMova.Application.DTOs.Autenticacion.SolicitarRecuperacionDto datos) => Task.CompletedTask;

        public Task RestablecerAsync(LFMova.Application.DTOs.Autenticacion.RestablecerContrasenaDto datos) => Task.CompletedTask;

        public Task EnviarInvitacionAsync(Usuario usuarioInvitado, string contextoInvitacion)
        {
            Invitaciones.Add((usuarioInvitado, contextoInvitacion));
            return Task.CompletedTask;
        }
    }

    private static EmpresaServicio CrearServicio(
        EmpresaRepositorioFalso? empresaRepo = null,
        UsuarioRepositorioFalso? usuarioRepo = null,
        UsuarioRolRepositorioFalso? rolRepo = null,
        RecuperacionContrasenaFalsa? recuperacion = null)
        => new(
            empresaRepo ?? new EmpresaRepositorioFalso(),
            usuarioRepo ?? new UsuarioRepositorioFalso(),
            rolRepo ?? new UsuarioRolRepositorioFalso(),
            recuperacion ?? new RecuperacionContrasenaFalsa());

    private static CrearEmpresaDto DatosValidos(string nombre = "Transportes ACME", string cif = "CIF-001", string cedulaCoordinador = "700")
        => new()
        {
            Nombre = nombre,
            Cif = cif,
            Direccion = "Calle Falsa 123",
            CedulaCoordinador = cedulaCoordinador,
            NombreCoordinador = "Coordinador de Prueba",
            TelefonoCoordinador = "3000000000",
            CorreoCoordinador = $"coordinador{cedulaCoordinador}@acme.test"
        };

    [Fact]
    public async Task CrearAsync_CreaLaEmpresaActivaConSuPrimerCoordinador()
    {
        var usuarioRepo = new UsuarioRepositorioFalso();
        var rolRepo = new UsuarioRolRepositorioFalso();
        var recuperacion = new RecuperacionContrasenaFalsa();
        var servicio = CrearServicio(usuarioRepo: usuarioRepo, rolRepo: rolRepo, recuperacion: recuperacion);

        var resultado = await servicio.CrearAsync(DatosValidos());

        Assert.True(resultado.Activa);
        Assert.Equal("Transportes ACME", resultado.Nombre);
        Assert.Equal("CIF-001", resultado.Cif);
        Assert.Equal("Calle Falsa 123", resultado.Direccion);
        Assert.True(resultado.EmpresaId > 0);

        var coordinador = await usuarioRepo.ObtenerPorCedulaConRolesAsync("700");
        Assert.NotNull(coordinador);
        Assert.Null(coordinador!.PasswordHash);
        Assert.False(coordinador.CorreoConfirmado);
        Assert.Equal("coordinador700@acme.test", coordinador.Email);
        Assert.Equal("Coordinador de Prueba", coordinador.NombreCompleto);
        var invitacion = Assert.Single(recuperacion.Invitaciones);
        Assert.Equal(coordinador.UsuarioId, invitacion.Usuario.UsuarioId);
        Assert.Single(rolRepo.Roles);
        Assert.Equal(Rol.COORDINADOR, rolRepo.Roles[0].Rol);
        Assert.Equal(resultado.EmpresaId, rolRepo.Roles[0].EmpresaId);
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoElCifYaExiste()
    {
        var empresaRepo = new EmpresaRepositorioFalso();
        var servicio = CrearServicio(empresaRepo);
        await servicio.CrearAsync(DatosValidos(cedulaCoordinador: "700"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.CrearAsync(DatosValidos(nombre: "Otra Empresa", cedulaCoordinador: "701")));
    }

    [Fact]
    public async Task CrearAsync_ReutilizaLaCuenta_CuandoLaCedulaDelCoordinadorYaTieneContrasena()
    {
        var usuarioExistente = new Usuario { UsuarioId = 5, Cedula = "800", PasswordHash = "hash-existente", Activo = true };
        var usuarioRepo = new UsuarioRepositorioFalso(usuarioExistente);
        var rolRepo = new UsuarioRolRepositorioFalso();
        var servicio = CrearServicio(usuarioRepo: usuarioRepo, rolRepo: rolRepo);

        await servicio.CrearAsync(DatosValidos(cedulaCoordinador: "800"));

        Assert.Contains(rolRepo.Roles, r => r.UsuarioId == 5 && r.Rol == Rol.COORDINADOR && r.Activo);
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoElCorreoDelCoordinadorNoEsValido()
    {
        var servicio = CrearServicio();
        var datos = DatosValidos();
        datos.CorreoCoordinador = "sin-arroba";

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearAsync(datos));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoLaDireccionEstaVacia()
    {
        var servicio = CrearServicio();
        var datos = DatosValidos();
        datos.Direccion = "";

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearAsync(datos));
    }

    [Fact]
    public async Task ObtenerTodasAsync_IncluyeLaCedulaDelCoordinadorPrincipalActivo()
    {
        var usuario = new Usuario { UsuarioId = 5, Cedula = "700", NombreCompleto = "Coordinador Uno", Activo = true };
        var empresaRepo = new EmpresaRepositorioFalso();
        await empresaRepo.AgregarAsync(new Empresa { Nombre = "ACME", Cif = "CIF-001", Direccion = "Calle Falsa 123", Activa = true });
        var rolRepo = new UsuarioRolRepositorioFalso();
        rolRepo.Roles.Add(new UsuarioRol { UsuarioRolId = 1, UsuarioId = 5, Usuario = usuario, Rol = Rol.COORDINADOR, EmpresaId = 1, Activo = true });
        var servicio = CrearServicio(empresaRepo, rolRepo: rolRepo);

        var todas = await servicio.ObtenerTodasAsync();

        var empresa = Assert.Single(todas);
        Assert.Equal(1, empresa.EmpresaId);
        Assert.Equal("700", empresa.CoordinadorPrincipal);
        Assert.Equal("Coordinador Uno", empresa.CoordinadorPrincipalNombre);
    }

    [Fact]
    public async Task ObtenerTodasAsync_CoordinadorPrincipalEsNulo_CuandoNoHayCoordinadorActivo()
    {
        var empresaRepo = new EmpresaRepositorioFalso();
        var servicio = CrearServicio(empresaRepo);
        await empresaRepo.AgregarAsync(new Empresa { Nombre = "Sin coordinador", Cif = "CIF-999", Direccion = "N/D", Activa = true });

        var todas = await servicio.ObtenerTodasAsync();

        var empresa = Assert.Single(todas);
        Assert.Null(empresa.CoordinadorPrincipal);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_DevuelveNulo_CuandoNoExiste()
    {
        var servicio = CrearServicio();

        var resultado = await servicio.ObtenerPorIdAsync(999);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task DesactivarAsync_DesactivaLaEmpresa()
    {
        var empresaRepo = new EmpresaRepositorioFalso();
        var servicio = CrearServicio(empresaRepo);
        var creada = await servicio.CrearAsync(DatosValidos());

        await servicio.DesactivarAsync(creada.EmpresaId);
        var actualizada = await servicio.ObtenerPorIdAsync(creada.EmpresaId);

        Assert.False(actualizada!.Activa);
    }

    [Fact]
    public async Task ActivarAsync_LanzaExcepcion_CuandoLaEmpresaNoExiste()
    {
        var servicio = CrearServicio();

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ActivarAsync(123));
    }
}
