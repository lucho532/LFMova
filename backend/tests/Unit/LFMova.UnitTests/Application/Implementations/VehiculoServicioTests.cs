using LFMova.Application.DTOs.Vehiculos;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;

namespace LFMova.UnitTests.Application.Implementations;

public class VehiculoServicioTests
{
    private class ConductorRepositorioFalso : IConductorRepositorio
    {
        private readonly Dictionary<int, Conductor> _conductores;

        public ConductorRepositorioFalso(params Conductor[] conductores)
            => _conductores = conductores.ToDictionary(c => c.ConductorId);

        public Task<Conductor?> ObtenerPorIdAsync(int conductorId)
            => Task.FromResult(_conductores.TryGetValue(conductorId, out var c) ? c : null);

        public Task<Conductor?> ObtenerPorUsuarioIdAsync(int usuarioId)
            => Task.FromResult(_conductores.Values.FirstOrDefault(c => c.UsuarioId == usuarioId));

        public Task<List<Conductor>> ObtenerPorEmpresaAsync(int empresaId) => Task.FromResult(new List<Conductor>());

        public Task AgregarAsync(Conductor conductor) => Task.CompletedTask;

        public Task AgregarVinculacionAsync(VinculacionConductorEmpresa vinculacion) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class VehiculoRepositorioFalso : IVehiculoRepositorio
    {
        private readonly Dictionary<int, Vehiculo> _vehiculos = new();
        private int _siguienteId = 1;

        public Task<Vehiculo?> ObtenerPorIdAsync(int vehiculoId)
            => Task.FromResult(_vehiculos.TryGetValue(vehiculoId, out var v) ? v : null);

        public Task<Vehiculo?> ObtenerPorPlacaAsync(string placa)
            => Task.FromResult(_vehiculos.Values.FirstOrDefault(v => v.Placa == placa));

        public Task<List<Vehiculo>> ObtenerPorConductorAsync(int conductorId)
            => Task.FromResult(_vehiculos.Values.Where(v => v.ConductorId == conductorId).ToList());

        public Task AgregarAsync(Vehiculo vehiculo)
        {
            vehiculo.VehiculoId = _siguienteId++;
            _vehiculos[vehiculo.VehiculoId] = vehiculo;
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class UnidadOperativaRepositorioFalso : IUnidadOperativaRepositorio
    {
        public readonly List<UnidadOperativa> Unidades = new();

        public Task<UnidadOperativa?> ObtenerPorIdAsync(int unidadOperativaId)
            => Task.FromResult(Unidades.FirstOrDefault(u => u.UnidadOperativaId == unidadOperativaId));

        public Task<UnidadOperativa?> ObtenerPorVehiculoAsync(int vehiculoId)
            => Task.FromResult(Unidades.FirstOrDefault(u => u.VehiculoId == vehiculoId));

        public Task<List<UnidadOperativa>> ObtenerPorConductorAsync(int conductorId)
            => Task.FromResult(Unidades.Where(u => u.ConductorId == conductorId).ToList());

        public Task AgregarAsync(UnidadOperativa unidadOperativa)
        {
            Unidades.Add(unidadOperativa);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task CrearAsync_AsignaElConductorYQuedaActivo()
    {
        var conductorRepo = new ConductorRepositorioFalso(new Conductor { ConductorId = 1, UsuarioId = 10, Activo = true });
        var servicio = new VehiculoServicio(new VehiculoRepositorioFalso(), conductorRepo, new UnidadOperativaRepositorioFalso());

        var vehiculo = await servicio.CrearAsync(1, new CrearVehiculoDto { Placa = "ABC123", Marca = "Chevrolet", Modelo = "2020", Capacidad = 4, VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1) });

        Assert.Equal(1, vehiculo.ConductorId);
        Assert.True(vehiculo.Activo);
    }

    private static CrearVehiculoDto DatosVehiculo(string placa = "ABC123", int capacidad = 4)
        => new() { Placa = placa, Marca = "Chevrolet", Modelo = "2020", Capacidad = capacidad, VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1) };

    [Fact]
    public async Task ActualizarAsync_CambiaLosDatosDelVehiculoDelConductor()
    {
        var conductorRepo = new ConductorRepositorioFalso(new Conductor { ConductorId = 1, UsuarioId = 10, Activo = true });
        var servicio = new VehiculoServicio(new VehiculoRepositorioFalso(), conductorRepo, new UnidadOperativaRepositorioFalso());
        var creado = await servicio.CrearAsync(1, DatosVehiculo());

        var actualizado = await servicio.ActualizarAsync(1, creado.VehiculoId, new CrearVehiculoDto
        {
            Placa = " XYZ987 ", Marca = "Renault", Modelo = "2023", Capacidad = 8, VigenciaSoat = new DateOnly(2028, 2, 2), VigenciaTecnomecanica = new DateOnly(2028, 3, 3)
        });

        Assert.Equal("XYZ987", actualizado.Placa);
        Assert.Equal("Renault", actualizado.Marca);
        Assert.Equal(8, actualizado.Capacidad);
        Assert.Equal(new DateOnly(2028, 2, 2), actualizado.VigenciaSoat);
        Assert.True(actualizado.Activo);
    }

    [Fact]
    public async Task ActualizarAsync_PermiteConservarLaMismaPlaca()
    {
        var conductorRepo = new ConductorRepositorioFalso(new Conductor { ConductorId = 1, UsuarioId = 10, Activo = true });
        var servicio = new VehiculoServicio(new VehiculoRepositorioFalso(), conductorRepo, new UnidadOperativaRepositorioFalso());
        var creado = await servicio.CrearAsync(1, DatosVehiculo());

        var actualizado = await servicio.ActualizarAsync(1, creado.VehiculoId, DatosVehiculo(capacidad: 6));

        Assert.Equal("ABC123", actualizado.Placa);
        Assert.Equal(6, actualizado.Capacidad);
    }

    [Fact]
    public async Task ActualizarAsync_LanzaExcepcion_CuandoLaPlacaYaEsDeOtroVehiculo()
    {
        var conductorRepo = new ConductorRepositorioFalso(new Conductor { ConductorId = 1, UsuarioId = 10, Activo = true });
        var servicio = new VehiculoServicio(new VehiculoRepositorioFalso(), conductorRepo, new UnidadOperativaRepositorioFalso());
        await servicio.CrearAsync(1, DatosVehiculo("AAA111"));
        var segundo = await servicio.CrearAsync(1, DatosVehiculo("BBB222"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ActualizarAsync(1, segundo.VehiculoId, DatosVehiculo("AAA111")));
    }

    [Fact]
    public async Task ActualizarAsync_LanzaExcepcion_CuandoElVehiculoEsDeOtroConductorOLosDatosNoSonValidos()
    {
        var conductorRepo = new ConductorRepositorioFalso(new Conductor { ConductorId = 1, UsuarioId = 10, Activo = true }, new Conductor { ConductorId = 2, UsuarioId = 11, Activo = true });
        var servicio = new VehiculoServicio(new VehiculoRepositorioFalso(), conductorRepo, new UnidadOperativaRepositorioFalso());
        var creado = await servicio.CrearAsync(1, DatosVehiculo());

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ActualizarAsync(2, creado.VehiculoId, DatosVehiculo()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ActualizarAsync(1, creado.VehiculoId, DatosVehiculo(capacidad: 0)));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoElConductorNoExiste()
    {
        var servicio = new VehiculoServicio(new VehiculoRepositorioFalso(), new ConductorRepositorioFalso(), new UnidadOperativaRepositorioFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(999, new CrearVehiculoDto { Placa = "ABC123", Marca = "Chevrolet", Modelo = "2020", Capacidad = 4, VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1) }));
    }

    [Fact]
    public async Task DesactivarAsync_DesactivaTambienLaUnidadOperativaActiva()
    {
        var conductorRepo = new ConductorRepositorioFalso(new Conductor { ConductorId = 1, UsuarioId = 10, Activo = true });
        var vehiculoRepo = new VehiculoRepositorioFalso();
        var unidadRepo = new UnidadOperativaRepositorioFalso();
        var servicio = new VehiculoServicio(vehiculoRepo, conductorRepo, unidadRepo);

        var vehiculo = await servicio.CrearAsync(1, new CrearVehiculoDto { Placa = "ABC123", Marca = "Chevrolet", Modelo = "2020", Capacidad = 4, VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1) });
        unidadRepo.Unidades.Add(new UnidadOperativa { UnidadOperativaId = 1, ConductorId = 1, VehiculoId = vehiculo.VehiculoId, Activa = true });

        await servicio.DesactivarAsync(1, vehiculo.VehiculoId);

        var vehiculoActualizado = await servicio.ObtenerPorIdAsync(vehiculo.VehiculoId);
        Assert.False(vehiculoActualizado!.Activo);
        Assert.False(unidadRepo.Unidades[0].Activa);
    }

    [Fact]
    public async Task DesactivarAsync_LanzaExcepcion_CuandoElVehiculoEsDeOtroConductor()
    {
        var conductorRepo = new ConductorRepositorioFalso(
            new Conductor { ConductorId = 1, UsuarioId = 10, Activo = true },
            new Conductor { ConductorId = 2, UsuarioId = 20, Activo = true });
        var vehiculoRepo = new VehiculoRepositorioFalso();
        var servicio = new VehiculoServicio(vehiculoRepo, conductorRepo, new UnidadOperativaRepositorioFalso());

        var vehiculo = await servicio.CrearAsync(1, new CrearVehiculoDto { Placa = "ABC123", Marca = "Chevrolet", Modelo = "2020", Capacidad = 4, VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1) });

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.DesactivarAsync(2, vehiculo.VehiculoId));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoLaCapacidadNoEsPositiva()
    {
        var conductorRepo = new ConductorRepositorioFalso(new Conductor { ConductorId = 1, UsuarioId = 10, Activo = true });
        var servicio = new VehiculoServicio(new VehiculoRepositorioFalso(), conductorRepo, new UnidadOperativaRepositorioFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(1, new CrearVehiculoDto { Placa = "ABC123", Marca = "Chevrolet", Modelo = "2020", Capacidad = 0, VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1) }));
    }
}
