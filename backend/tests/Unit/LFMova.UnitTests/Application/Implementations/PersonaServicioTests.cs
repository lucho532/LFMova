using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

public class PersonaServicioTests
{
    private class UsuarioRepositorioFalso : IUsuarioRepositorio
    {
        private readonly Usuario? _usuario;

        public UsuarioRepositorioFalso(Usuario? usuario) => _usuario = usuario;

        public Task<Usuario?> ObtenerPorIdAsync(int usuarioId) => Task.FromResult(_usuario);

        public Task<Usuario?> ObtenerPorCedulaConRolesAsync(string cedula) => Task.FromResult(_usuario is not null && _usuario.Cedula == cedula ? _usuario : null);

        public Task<Usuario?> ObtenerPorIdentificadorConRolesAsync(string identificador) => Task.FromResult(_usuario);

        public Task<Usuario?> ObtenerPorEmailAsync(string email) => Task.FromResult(_usuario);

        public Task AgregarAsync(Usuario usuario) => Task.CompletedTask;

        public void Descartar(Usuario usuario) { }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }


    private class EmpresaRepositorioFalso : IEmpresaRepositorio
    {
        private readonly List<Empresa> _empresas;

        public EmpresaRepositorioFalso(params Empresa[] empresas) => _empresas = empresas.ToList();

        public Task<Empresa?> ObtenerPorIdAsync(int empresaId) => Task.FromResult(_empresas.FirstOrDefault(e => e.EmpresaId == empresaId));

        public Task<Empresa?> ObtenerPorCifAsync(string cif) => Task.FromResult<Empresa?>(null);

        public Task<List<Empresa>> ObtenerTodasAsync() => Task.FromResult(_empresas);

        public Task AgregarAsync(Empresa empresa) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class ConductorRepositorioFalso : IConductorRepositorio
    {
        private readonly Conductor? _conductor;

        public ConductorRepositorioFalso(Conductor? conductor = null) => _conductor = conductor;

        public Task<Conductor?> ObtenerPorIdAsync(int conductorId) => Task.FromResult(_conductor);

        public Task<Conductor?> ObtenerPorUsuarioIdAsync(int usuarioId) => Task.FromResult(_conductor);

        public Task<List<Conductor>> ObtenerPorEmpresaAsync(int empresaId) => Task.FromResult(new List<Conductor>());

        public Task AgregarAsync(Conductor conductor) => Task.CompletedTask;

        public Task AgregarVinculacionAsync(VinculacionConductorEmpresa vinculacion) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class VehiculoRepositorioFalso : IVehiculoRepositorio
    {
        private readonly List<Vehiculo> _vehiculos;

        public VehiculoRepositorioFalso(params Vehiculo[] vehiculos) => _vehiculos = vehiculos.ToList();

        public Task<Vehiculo?> ObtenerPorIdAsync(int vehiculoId) => Task.FromResult(_vehiculos.FirstOrDefault(v => v.VehiculoId == vehiculoId));

        public Task<List<Vehiculo>> ObtenerPorConductorAsync(int conductorId) => Task.FromResult(_vehiculos);

        public Task<Vehiculo?> ObtenerPorPlacaAsync(string placa) => Task.FromResult<Vehiculo?>(null);

        public Task AgregarAsync(Vehiculo vehiculo) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class EmpleadoRepositorioFalso : IEmpleadoRepositorio
    {
        private readonly Empleado? _empleado;

        public EmpleadoRepositorioFalso(Empleado? empleado = null) => _empleado = empleado;

        public Task<Empleado?> ObtenerPorIdAsync(int empleadoId) => Task.FromResult(_empleado);

        public Task<Empleado?> ObtenerPorUsuarioIdAsync(int usuarioId) => Task.FromResult(_empleado);

        public Task<List<Empleado>> ObtenerPorEmpresaAsync(int empresaId) => Task.FromResult(new List<Empleado>());

        public Task AgregarAsync(Empleado empleado) => Task.CompletedTask;

        public void Descartar(Empleado empleado) { }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class InvitacionRepositorioFalso : IInvitacionEmpresaRepositorio
    {
        private readonly List<InvitacionEmpresa> _invitaciones;

        public InvitacionRepositorioFalso(IEnumerable<InvitacionEmpresa> invitaciones) => _invitaciones = invitaciones.ToList();

        public Task<InvitacionEmpresa?> ObtenerPorIdAsync(int invitacionEmpresaId)
            => Task.FromResult(_invitaciones.FirstOrDefault(i => i.InvitacionEmpresaId == invitacionEmpresaId));

        public Task<List<InvitacionEmpresa>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_invitaciones.Where(i => i.EmpresaId == empresaId).ToList());

        public Task<List<InvitacionEmpresa>> ObtenerPorEmpresaYCedulaAsync(int empresaId, string cedula)
            => Task.FromResult(_invitaciones.Where(i => i.EmpresaId == empresaId && i.Cedula == cedula).ToList());

        public Task AgregarAsync(InvitacionEmpresa invitacion)
        {
            _invitaciones.Add(invitacion);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private static PersonaServicio CrearServicio(
        Usuario usuario, Empresa[]? empresas = null, Conductor? conductor = null, Vehiculo[]? vehiculos = null, Empleado? empleado = null,
        InvitacionEmpresa[]? invitaciones = null)
        => new(
            new UsuarioRepositorioFalso(usuario),
            new EmpresaRepositorioFalso(empresas ?? Array.Empty<Empresa>()),
            new ConductorRepositorioFalso(conductor),
            new VehiculoRepositorioFalso(vehiculos ?? Array.Empty<Vehiculo>()),
            new EmpleadoRepositorioFalso(empleado),
            new InvitacionRepositorioFalso(invitaciones ?? Array.Empty<InvitacionEmpresa>()));

    private static Usuario CrearUsuario()
    {
        var usuario = new Usuario { UsuarioId = 1, Cedula = "1001", NombreCompleto = "Ana Gómez", Email = "ana@test.com", Telefono = "3000000000", Activo = true };
        usuario.UsuarioRoles.Add(new UsuarioRol { Rol = Rol.EMPLEADO, Activo = true });
        return usuario;
    }

    [Fact]
    public async Task BuscarPorCedulaAsync_DevuelveLosDatosDeLaPersona()
    {
        var servicio = CrearServicio(CrearUsuario());

        var persona = await servicio.BuscarPorCedulaAsync("1001");

        Assert.Equal("Ana Gómez", persona.NombreCompleto);
        Assert.Equal("ana@test.com", persona.Email);
        Assert.False(persona.EsConductor);
        Assert.False(persona.EsCoordinador);
    }

    [Fact]
    public async Task BuscarPorCedulaAsync_IndicaSiYaEsConductorOCoordinador()
    {
        var usuario = CrearUsuario();
        usuario.UsuarioRoles.Add(new UsuarioRol { Rol = Rol.CONDUCTOR, Activo = true });
        usuario.UsuarioRoles.Add(new UsuarioRol { Rol = Rol.COORDINADOR, EmpresaId = 5, Activo = false });
        var servicio = CrearServicio(usuario);

        var persona = await servicio.BuscarPorCedulaAsync("1001");

        Assert.True(persona.EsConductor);
        Assert.False(persona.EsCoordinador);
    }

    [Fact]
    public async Task BuscarPorCedulaAsync_LanzaExcepcion_CuandoNoExisteNadieConEsaCedula()
    {
        var servicio = CrearServicio(CrearUsuario());

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.BuscarPorCedulaAsync("9999"));
    }

    [Fact]
    public async Task BuscarPorCedulaAsync_LanzaExcepcion_CuandoLaCedulaEstaVacia()
    {
        var servicio = CrearServicio(CrearUsuario());

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.BuscarPorCedulaAsync(" "));
    }

    [Fact]
    public async Task BuscarPorCedulaAsync_IncluyeLasEmpresasVehiculosYPerfilDeEmpleado()
    {
        var usuario = CrearUsuario();
        usuario.UsuarioRoles.Add(new UsuarioRol { Rol = Rol.COORDINADOR, EmpresaId = 5, Activo = true });
        var conductor = new Conductor { ConductorId = 3, UsuarioId = 1, Activo = true };
        conductor.VinculacionesConductorEmpresa.Add(new VinculacionConductorEmpresa { ConductorId = 3, EmpresaId = 6, Activa = true });
        conductor.VinculacionesConductorEmpresa.Add(new VinculacionConductorEmpresa { ConductorId = 3, EmpresaId = 5, Activa = false });
        var servicio = CrearServicio(
            usuario,
            empresas: new[] { new Empresa { EmpresaId = 5, Nombre = "Cinco" }, new Empresa { EmpresaId = 6, Nombre = "Seis" } },
            conductor: conductor,
            vehiculos: new[] { new Vehiculo { VehiculoId = 1, Placa = "ABC123", Activo = true }, new Vehiculo { VehiculoId = 2, Placa = "OLD999", Activo = false } },
            empleado: new Empleado { EmpleadoId = 9, UsuarioId = 1, EmpresaId = 6 });

        var persona = await servicio.BuscarPorCedulaAsync("1001");

        Assert.Equal("Cinco", persona.EmpresaCoordinada?.Nombre);
        Assert.Equal(new[] { "Seis" }, persona.EmpresasConductor.Select(e => e.Nombre));
        Assert.Equal(new[] { "ABC123" }, persona.Placas);
        Assert.Equal("Seis", persona.EmpresaEmpleado?.Nombre);
    }

    [Fact]
    public async Task BuscarPorCedulaAsync_RestringidaAUnaEmpresa_NoEncuentraAUnaPersonaAjenaAEsaEmpresa()
    {
        var servicio = CrearServicio(CrearUsuario(), empleado: new Empleado { EmpleadoId = 9, UsuarioId = 1, EmpresaId = 6 });

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.BuscarPorCedulaAsync("1001", empresaIdRestringida: 5));

        // Mismo mensaje que si la cédula no existiera: no revela que la persona tiene cuenta.
        Assert.Equal("No existe ninguna persona registrada con esa cédula.", excepcion.Message);
    }

    [Fact]
    public async Task BuscarPorCedulaAsync_RestringidaAUnaEmpresa_EncuentraASuEmpleado()
    {
        var servicio = CrearServicio(CrearUsuario(), empleado: new Empleado { EmpleadoId = 9, UsuarioId = 1, EmpresaId = 5 });

        var persona = await servicio.BuscarPorCedulaAsync("1001", empresaIdRestringida: 5);

        Assert.Equal("Ana Gómez", persona.NombreCompleto);
    }

    [Fact]
    public async Task EstaRelacionadaConEmpresaAsync_EsVerdadero_ParaUnConductorVinculado()
    {
        var conductor = new Conductor { ConductorId = 3, UsuarioId = 1, Activo = true };
        conductor.VinculacionesConductorEmpresa.Add(new VinculacionConductorEmpresa { ConductorId = 3, EmpresaId = 5, Activa = true });
        var servicio = CrearServicio(CrearUsuario(), conductor: conductor);

        Assert.True(await servicio.EstaRelacionadaConEmpresaAsync("1001", 5));
        Assert.False(await servicio.EstaRelacionadaConEmpresaAsync("1001", 6));
    }

    [Fact]
    public async Task EstaRelacionadaConEmpresaAsync_SoloCuentaUnaInvitacionAceptada()
    {
        var pendiente = new InvitacionEmpresa { InvitacionEmpresaId = 1, EmpresaId = 5, Cedula = "1001", FechaExpiracion = DateTime.UtcNow.AddDays(3) };
        var aceptada = new InvitacionEmpresa { InvitacionEmpresaId = 2, EmpresaId = 7, Cedula = "1001", FechaAceptacion = DateTime.UtcNow, UsuarioAceptanteId = 1 };
        var servicio = CrearServicio(CrearUsuario(), invitaciones: new[] { pendiente, aceptada });

        Assert.False(await servicio.EstaRelacionadaConEmpresaAsync("1001", 5));
        Assert.True(await servicio.EstaRelacionadaConEmpresaAsync("1001", 7));
    }

    [Fact]
    public async Task PuedeGestionarseDesdeEmpresaAsync_PermiteUnaCedulaSinCuenta_YBloqueaUnaCuentaAjena()
    {
        var servicio = CrearServicio(CrearUsuario());

        Assert.True(await servicio.PuedeGestionarseDesdeEmpresaAsync("9999", 5));
        Assert.False(await servicio.PuedeGestionarseDesdeEmpresaAsync("1001", 5));
    }
}
