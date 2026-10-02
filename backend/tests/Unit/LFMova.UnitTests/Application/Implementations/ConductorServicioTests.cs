using LFMova.Application.DTOs.Conductores;
using LFMova.Application.DTOs.Vehiculos;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

public class ConductorServicioTests
{
    private class UsuarioRolRepositorioFalso : IUsuarioRolRepositorio
    {
        private readonly Dictionary<int, UsuarioRol> _roles = new();
        private int _siguienteId = 1;

        public Task<UsuarioRol?> ObtenerPorIdAsync(int usuarioRolId)
            => Task.FromResult(_roles.TryGetValue(usuarioRolId, out var r) ? r : null);

        public Task<List<UsuarioRol>> ObtenerPorEmpresaYRolAsync(int empresaId, Rol rol)
            => Task.FromResult(_roles.Values.Where(r => r.EmpresaId == empresaId && r.Rol == rol).ToList());

        public Task AgregarAsync(UsuarioRol usuarioRol)
        {
            usuarioRol.UsuarioRolId = _siguienteId++;
            _roles[usuarioRol.UsuarioRolId] = usuarioRol;
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

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

    private class ConductorRepositorioFalso : IConductorRepositorio
    {
        private readonly Dictionary<int, Conductor> _conductores = new();
        private readonly List<VinculacionConductorEmpresa> _vinculaciones = new();
        private int _siguienteConductorId = 1;
        private int _siguienteVinculacionId = 1;

        public Task<Conductor?> ObtenerPorIdAsync(int conductorId)
            => Task.FromResult(ConVinculaciones(_conductores.TryGetValue(conductorId, out var c) ? c : null));

        public Task<Conductor?> ObtenerPorUsuarioIdAsync(int usuarioId)
            => Task.FromResult(ConVinculaciones(_conductores.Values.FirstOrDefault(c => c.UsuarioId == usuarioId)));

        public Task<List<Conductor>> ObtenerPorEmpresaAsync(int empresaId)
        {
            var conductorIds = _vinculaciones.Where(v => v.EmpresaId == empresaId).Select(v => v.ConductorId).ToHashSet();
            return Task.FromResult(_conductores.Values.Where(c => conductorIds.Contains(c.ConductorId)).Select(c => ConVinculaciones(c)!).ToList());
        }

        public Task AgregarAsync(Conductor conductor)
        {
            conductor.ConductorId = _siguienteConductorId++;
            _conductores[conductor.ConductorId] = conductor;
            return Task.CompletedTask;
        }

        public Task AgregarVinculacionAsync(VinculacionConductorEmpresa vinculacion)
        {
            vinculacion.VinculacionConductorEmpresaId = _siguienteVinculacionId++;
            _vinculaciones.Add(vinculacion);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;

        private Conductor? ConVinculaciones(Conductor? conductor)
        {
            if (conductor is null)
            {
                return null;
            }

            conductor.VinculacionesConductorEmpresa = _vinculaciones.Where(v => v.ConductorId == conductor.ConductorId).ToList();
            return conductor;
        }
    }

    private class UnidadOperativaRepositorioFalso : IUnidadOperativaRepositorio
    {
        private readonly List<UnidadOperativa> _unidades;

        public UnidadOperativaRepositorioFalso(params UnidadOperativa[] unidades) => _unidades = unidades.ToList();

        public Task<UnidadOperativa?> ObtenerPorIdAsync(int unidadOperativaId)
            => Task.FromResult(_unidades.FirstOrDefault(u => u.UnidadOperativaId == unidadOperativaId));

        public Task<UnidadOperativa?> ObtenerPorVehiculoAsync(int vehiculoId)
            => Task.FromResult(_unidades.FirstOrDefault(u => u.VehiculoId == vehiculoId));

        public Task<List<UnidadOperativa>> ObtenerPorConductorAsync(int conductorId)
            => Task.FromResult(_unidades.Where(u => u.ConductorId == conductorId).ToList());

        public Task AgregarAsync(UnidadOperativa unidadOperativa)
        {
            _unidades.Add(unidadOperativa);
            return Task.CompletedTask;
        }

        public IReadOnlyList<UnidadOperativa> Registradas => _unidades;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class ServicioRepositorioFalso : IServicioRepositorio
    {
        private readonly List<Servicio> _servicios;

        public ServicioRepositorioFalso(params Servicio[] servicios) => _servicios = servicios.ToList();

        public Task<Servicio?> ObtenerPorIdAsync(int servicioId)
            => Task.FromResult(_servicios.FirstOrDefault(s => s.ServicioId == servicioId));

        public Task<List<Servicio>> ObtenerPorJornadaAsync(int jornadaId)
            => Task.FromResult(_servicios.Where(s => s.JornadaId == jornadaId).ToList());

        public Task<List<Servicio>> ObtenerPorUnidadOperativaAsync(int unidadOperativaId)
            => Task.FromResult(_servicios.Where(s => s.UnidadOperativaId == unidadOperativaId).ToList());

        public Task<List<Servicio>> ObtenerPublicadosPorTipoAsync(TipoServicio tipo)
            => Task.FromResult(_servicios.Where(s => s.Estado == EstadoServicio.PUBLICADO && s.Tipo == tipo).ToList());


        public Task<List<Servicio>> ObtenerPendientesDeProgramacionAsync(int empresaId, DateOnly desde) => Task.FromResult(new List<Servicio>());



        public Task<List<Servicio>> ObtenerActivosPorFechaYHoraAsync(DateOnly fecha, TimeOnly hora) => Task.FromResult(new List<Servicio>());

        public Task AgregarAsync(Servicio servicio) => Task.CompletedTask;

        public Task EliminarAsync(Servicio servicio) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class VehiculoRepositorioFalso : IVehiculoRepositorio
    {
        public readonly List<Vehiculo> Vehiculos = new();
        private int _siguienteId = 1;

        public Task<Vehiculo?> ObtenerPorIdAsync(int vehiculoId) => Task.FromResult(Vehiculos.FirstOrDefault(v => v.VehiculoId == vehiculoId));

        public Task<List<Vehiculo>> ObtenerPorConductorAsync(int conductorId) => Task.FromResult(Vehiculos.Where(v => v.ConductorId == conductorId).ToList());

        public Task<Vehiculo?> ObtenerPorPlacaAsync(string placa) => Task.FromResult(Vehiculos.FirstOrDefault(v => v.Placa == placa));

        public Task AgregarAsync(Vehiculo vehiculo)
        {
            vehiculo.VehiculoId = _siguienteId++;
            Vehiculos.Add(vehiculo);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private static CrearVehiculoDto VehiculoValido(string placa = "ABC123") => new()
    {
        Placa = placa,
        Marca = "Chevrolet",
        Modelo = "2022",
        Capacidad = 4,
        VigenciaSoat = new DateOnly(2027, 1, 1),
        VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
    };

    private static UsuarioRepositorioFalso UsuarioRegistrado()
        => new(new Usuario
        {
            UsuarioId = 50,
            Cedula = "555",
            Email = "juan@test.com",
            NombreCompleto = "Juan Pérez",
            Telefono = "3000000000",
            PasswordHash = "hash",
            CorreoConfirmado = true,
            Activo = true
        });

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoLaPersonaNoSeHaRegistrado()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var servicio = ConductorServicioFabrica.Crear(
            new ConductorRepositorioFalso(), new UsuarioRepositorioFalso(), empresaRepo, new UsuarioRolRepositorioFalso(),
            new UnidadOperativaRepositorioFalso(), new ServicioRepositorioFalso(), new VehiculoRepositorioFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(1, new CrearConductorDto { Cedula = "555", Vehiculo = VehiculoValido() }));
    }

    [Fact]
    public async Task CrearAsync_AsignaRolConductorYVinculacion_CuandoLaPersonaYaTieneCuenta()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var usuarioRepo = UsuarioRegistrado();
        var servicio = ConductorServicioFabrica.Crear(new ConductorRepositorioFalso(), usuarioRepo, empresaRepo, new UsuarioRolRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ServicioRepositorioFalso(), new VehiculoRepositorioFalso());

        var conductor = await servicio.CrearAsync(1, new CrearConductorDto { Cedula = "555", Vehiculo = VehiculoValido() });

        var usuarioCreado = await usuarioRepo.ObtenerPorCedulaConRolesAsync("555");
        Assert.NotNull(usuarioCreado);
        Assert.Equal("Juan Pérez", conductor.NombreCompleto);
        Assert.True(conductor.Activo);
        Assert.Contains(1, conductor.EmpresaIdsVinculadosActivos);
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoLaCedulaYaEsConductor()
    {
        var empresaRepo = new EmpresaRepositorioFalso(
            new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true },
            new Empresa { EmpresaId = 2, Nombre = "Otra", Activa = true });
        var usuarioRepo = UsuarioRegistrado();
        var servicio = ConductorServicioFabrica.Crear(new ConductorRepositorioFalso(), usuarioRepo, empresaRepo, new UsuarioRolRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ServicioRepositorioFalso(), new VehiculoRepositorioFalso());

        await servicio.CrearAsync(1, new CrearConductorDto { Cedula = "555", Vehiculo = VehiculoValido() });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(2, new CrearConductorDto { Cedula = "555", Vehiculo = VehiculoValido() }));
    }

    [Fact]
    public async Task VincularAsync_CreaVinculacion_CuandoElConductorYaExisteEnOtraEmpresa()
    {
        var empresaRepo = new EmpresaRepositorioFalso(
            new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true },
            new Empresa { EmpresaId = 2, Nombre = "Otra", Activa = true });
        var usuarioRepo = UsuarioRegistrado();
        var servicio = ConductorServicioFabrica.Crear(new ConductorRepositorioFalso(), usuarioRepo, empresaRepo, new UsuarioRolRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ServicioRepositorioFalso(), new VehiculoRepositorioFalso());

        await servicio.CrearAsync(1, new CrearConductorDto { Cedula = "555", Vehiculo = VehiculoValido() });
        var vinculacion = await servicio.VincularAsync(2, new VincularConductorDto { Cedula = "555" });

        Assert.Equal(2, vinculacion.EmpresaId);
        Assert.True(vinculacion.Activa);

        var conductorActualizado = await servicio.ObtenerPorIdAsync(vinculacion.ConductorId);
        Assert.Equal(new[] { 1, 2 }, conductorActualizado!.EmpresaIdsVinculadosActivos.OrderBy(e => e));
    }

    [Fact]
    public async Task VincularAsync_LanzaExcepcion_CuandoElConductorNoExiste()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var servicio = ConductorServicioFabrica.Crear(new ConductorRepositorioFalso(), UsuarioRegistrado(), empresaRepo, new UsuarioRolRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ServicioRepositorioFalso(), new VehiculoRepositorioFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.VincularAsync(1, new VincularConductorDto { Cedula = "no-existe" }));
    }

    [Fact]
    public async Task VincularAsync_LanzaExcepcion_CuandoYaExisteVinculacionConEsaEmpresa()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var usuarioRepo = UsuarioRegistrado();
        var servicio = ConductorServicioFabrica.Crear(new ConductorRepositorioFalso(), usuarioRepo, empresaRepo, new UsuarioRolRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ServicioRepositorioFalso(), new VehiculoRepositorioFalso());

        await servicio.CrearAsync(1, new CrearConductorDto { Cedula = "555", Vehiculo = VehiculoValido() });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.VincularAsync(1, new VincularConductorDto { Cedula = "555" }));
    }

    [Fact]
    public async Task DesactivarVinculacionAsync_Desactiva_CuandoLaVinculacionExiste()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var usuarioRepo = UsuarioRegistrado();
        var servicio = ConductorServicioFabrica.Crear(new ConductorRepositorioFalso(), usuarioRepo, empresaRepo, new UsuarioRolRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ServicioRepositorioFalso(), new VehiculoRepositorioFalso());
        var conductor = await servicio.CrearAsync(1, new CrearConductorDto { Cedula = "555", Vehiculo = VehiculoValido() });

        await servicio.DesactivarVinculacionAsync(1, conductor.ConductorId);
        var actualizado = await servicio.ObtenerPorIdAsync(conductor.ConductorId);

        Assert.DoesNotContain(1, actualizado!.EmpresaIdsVinculadosActivos);
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoLaCedulaEstaVacia()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var servicio = ConductorServicioFabrica.Crear(new ConductorRepositorioFalso(), UsuarioRegistrado(), empresaRepo, new UsuarioRolRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ServicioRepositorioFalso(), new VehiculoRepositorioFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(1, new CrearConductorDto { Cedula = "", Vehiculo = VehiculoValido() }));
    }

    [Fact]
    public async Task VincularAsync_LanzaExcepcion_CuandoLaCedulaEstaVacia()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var servicio = ConductorServicioFabrica.Crear(new ConductorRepositorioFalso(), UsuarioRegistrado(), empresaRepo, new UsuarioRolRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ServicioRepositorioFalso(), new VehiculoRepositorioFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.VincularAsync(1, new VincularConductorDto { Cedula = "   " }));
    }

    [Fact]
    public async Task ObtenerServiciosPropiosAsync_DevuelveServiciosDeSusUnidadesOperativas()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var usuarioRepo = UsuarioRegistrado();
        var conductorRepo = new ConductorRepositorioFalso();
        var servicioDeCreacion = ConductorServicioFabrica.Crear(
            conductorRepo, usuarioRepo, empresaRepo, new UsuarioRolRepositorioFalso(), new UnidadOperativaRepositorioFalso(), new ServicioRepositorioFalso(), new VehiculoRepositorioFalso());
        var conductor = await servicioDeCreacion.CrearAsync(1, new CrearConductorDto { Cedula = "555", Vehiculo = VehiculoValido() });

        var unidad = new UnidadOperativa { UnidadOperativaId = 1, ConductorId = conductor.ConductorId, VehiculoId = 1, Activa = true };
        var servicioAsignado = new Servicio
        {
            ServicioId = 10,
            JornadaId = 1,
            UnidadOperativaId = 1,
            SedeId = 1,
            Fecha = new DateOnly(2026, 1, 1),
            HoraProgramada = new TimeOnly(6, 0),
            Tipo = TipoServicio.ENTRADA,
            Estado = EstadoServicio.PUBLICADO,
            Jornada = new Jornada { JornadaId = 1, EmpresaId = 1, FechaOperativa = new DateOnly(2026, 1, 1) }
        };

        // Un servicio asignado pero aún no publicado no debe verlo el conductor.
        var servicioSinPublicar = new Servicio
        {
            ServicioId = 11, JornadaId = 1, UnidadOperativaId = 1, SedeId = 1, Fecha = new DateOnly(2026, 1, 2), HoraProgramada = new TimeOnly(7, 0),
            Tipo = TipoServicio.ENTRADA, Estado = EstadoServicio.ASIGNADO, Jornada = servicioAsignado.Jornada
        };

        var servicio = ConductorServicioFabrica.Crear(
            conductorRepo, usuarioRepo, empresaRepo, new UsuarioRolRepositorioFalso(),
            new UnidadOperativaRepositorioFalso(unidad), new ServicioRepositorioFalso(servicioAsignado, servicioSinPublicar), new VehiculoRepositorioFalso());

        var resultado = await servicio.ObtenerServiciosPropiosAsync(conductor.UsuarioId);

        var servicioDto = Assert.Single(resultado);
        Assert.Equal(10, servicioDto.ServicioId);
        Assert.Equal(1, servicioDto.EmpresaId);
    }

    [Fact]
    public async Task ObtenerServiciosPropiosAsync_LanzaExcepcion_CuandoNoTienePerfilDeConductor()
    {
        var empresaRepo = new EmpresaRepositorioFalso();
        var servicio = ConductorServicioFabrica.Crear(
            new ConductorRepositorioFalso(), UsuarioRegistrado(), empresaRepo, new UsuarioRolRepositorioFalso(),
            new UnidadOperativaRepositorioFalso(), new ServicioRepositorioFalso(), new VehiculoRepositorioFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ObtenerServiciosPropiosAsync(999));
    }

    [Fact]
    public async Task CrearAsync_RegistraElVehiculoYFormaLaUnidadOperativa()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var vehiculos = new VehiculoRepositorioFalso();
        var unidades = new UnidadOperativaRepositorioFalso();
        var servicio = ConductorServicioFabrica.Crear(
            new ConductorRepositorioFalso(), UsuarioRegistrado(), empresaRepo, new UsuarioRolRepositorioFalso(), unidades, new ServicioRepositorioFalso(), vehiculos);

        var conductor = await servicio.CrearAsync(1, new CrearConductorDto { Cedula = "555", Vehiculo = VehiculoValido("XYZ987") });

        var vehiculo = Assert.Single(vehiculos.Vehiculos);
        Assert.Equal("XYZ987", vehiculo.Placa);
        Assert.Equal(conductor.ConductorId, vehiculo.ConductorId);
        Assert.Equal(new DateOnly(2027, 1, 1), vehiculo.VigenciaSoat);
        var unidad = Assert.Single(unidades.Registradas);
        Assert.Equal(vehiculo.VehiculoId, unidad.VehiculoId);
        Assert.True(unidad.Activa);
    }

    [Fact]
    public async Task ObtenerUnidadesPropiasAsync_DevuelveLaUnidadConLosDatosDelVehiculo()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var usuarioRepo = UsuarioRegistrado();
        var conductorRepo = new ConductorRepositorioFalso();
        var vehiculos = new VehiculoRepositorioFalso();
        var unidades = new UnidadOperativaRepositorioFalso();
        var servicio = ConductorServicioFabrica.Crear(
            conductorRepo, usuarioRepo, empresaRepo, new UsuarioRolRepositorioFalso(), unidades, new ServicioRepositorioFalso(), vehiculos);
        var conductor = await servicio.CrearAsync(1, new CrearConductorDto { Cedula = "555", Vehiculo = VehiculoValido("XYZ987") });

        var resultado = await servicio.ObtenerUnidadesPropiasAsync(conductor.UsuarioId);

        var unidad = Assert.Single(resultado);
        Assert.Equal("XYZ987", unidad.Vehiculo.Placa);
        Assert.Equal(4, unidad.Vehiculo.Capacidad);
        Assert.Equal(new DateOnly(2027, 6, 1), unidad.Vehiculo.VigenciaTecnomecanica);
    }

    [Fact]
    public async Task ObtenerUnidadesPropiasAsync_LanzaExcepcion_CuandoNoTienePerfilDeConductor()
    {
        var servicio = ConductorServicioFabrica.Crear(
            new ConductorRepositorioFalso(), new UsuarioRepositorioFalso(), new EmpresaRepositorioFalso(), new UsuarioRolRepositorioFalso(),
            new UnidadOperativaRepositorioFalso(), new ServicioRepositorioFalso(), new VehiculoRepositorioFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ObtenerUnidadesPropiasAsync(999));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoFaltaLaVigenciaDelSoat()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var servicio = ConductorServicioFabrica.Crear(
            new ConductorRepositorioFalso(), UsuarioRegistrado(), empresaRepo, new UsuarioRolRepositorioFalso(),
            new UnidadOperativaRepositorioFalso(), new ServicioRepositorioFalso(), new VehiculoRepositorioFalso());
        var vehiculo = VehiculoValido();
        vehiculo.VigenciaSoat = default;

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearAsync(1, new CrearConductorDto { Cedula = "555", Vehiculo = vehiculo }));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoLaPlacaYaEstaRegistrada()
    {
        var empresaRepo = new EmpresaRepositorioFalso(new Empresa { EmpresaId = 1, Nombre = "ACME", Activa = true });
        var vehiculos = new VehiculoRepositorioFalso();
        await vehiculos.AgregarAsync(new Vehiculo { Placa = "ABC123", ConductorId = 99 });
        var servicio = ConductorServicioFabrica.Crear(
            new ConductorRepositorioFalso(), UsuarioRegistrado(), empresaRepo, new UsuarioRolRepositorioFalso(),
            new UnidadOperativaRepositorioFalso(), new ServicioRepositorioFalso(), vehiculos);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearAsync(1, new CrearConductorDto { Cedula = "555", Vehiculo = VehiculoValido("ABC123") }));
    }
}
