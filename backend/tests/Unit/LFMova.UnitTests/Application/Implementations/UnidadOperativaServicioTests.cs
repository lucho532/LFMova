using LFMova.Application.DTOs.UnidadesOperativas;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;

namespace LFMova.UnitTests.Application.Implementations;

public class UnidadOperativaServicioTests
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
        private readonly Dictionary<int, Vehiculo> _vehiculos;

        public VehiculoRepositorioFalso(params Vehiculo[] vehiculos)
            => _vehiculos = vehiculos.ToDictionary(v => v.VehiculoId);

        public Task<Vehiculo?> ObtenerPorIdAsync(int vehiculoId)
            => Task.FromResult(_vehiculos.TryGetValue(vehiculoId, out var v) ? v : null);

        public Task<Vehiculo?> ObtenerPorPlacaAsync(string placa)
            => Task.FromResult(_vehiculos.Values.FirstOrDefault(v => v.Placa == placa));

        public Task<List<Vehiculo>> ObtenerPorConductorAsync(int conductorId)
            => Task.FromResult(_vehiculos.Values.Where(v => v.ConductorId == conductorId).ToList());

        public Task AgregarAsync(Vehiculo vehiculo) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class UnidadOperativaRepositorioFalso : IUnidadOperativaRepositorio
    {
        private readonly List<UnidadOperativa> _unidades = new();
        private int _siguienteId = 1;

        public Task<UnidadOperativa?> ObtenerPorIdAsync(int unidadOperativaId)
            => Task.FromResult(_unidades.FirstOrDefault(u => u.UnidadOperativaId == unidadOperativaId));

        public Task<UnidadOperativa?> ObtenerPorVehiculoAsync(int vehiculoId)
            => Task.FromResult(_unidades.FirstOrDefault(u => u.VehiculoId == vehiculoId));

        public Task<List<UnidadOperativa>> ObtenerPorConductorAsync(int conductorId)
            => Task.FromResult(_unidades.Where(u => u.ConductorId == conductorId).ToList());

        public Task AgregarAsync(UnidadOperativa unidadOperativa)
        {
            unidadOperativa.UnidadOperativaId = _siguienteId++;
            _unidades.Add(unidadOperativa);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task CrearAsync_Crea_CuandoElVehiculoPerteneceAlConductor()
    {
        var conductorRepo = new ConductorRepositorioFalso(new Conductor { ConductorId = 1, UsuarioId = 10, Activo = true });
        var vehiculoRepo = new VehiculoRepositorioFalso(new Vehiculo { VehiculoId = 5, ConductorId = 1, Activo = true });
        var servicio = new UnidadOperativaServicio(new UnidadOperativaRepositorioFalso(), vehiculoRepo, conductorRepo);

        var unidad = await servicio.CrearAsync(1, new CrearUnidadOperativaDto { VehiculoId = 5 });

        Assert.Equal(1, unidad.ConductorId);
        Assert.Equal(5, unidad.VehiculoId);
        Assert.True(unidad.Activa);
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoElVehiculoNoPerteneceAlConductor()
    {
        var conductorRepo = new ConductorRepositorioFalso(
            new Conductor { ConductorId = 1, UsuarioId = 10, Activo = true },
            new Conductor { ConductorId = 2, UsuarioId = 20, Activo = true });
        var vehiculoRepo = new VehiculoRepositorioFalso(new Vehiculo { VehiculoId = 5, ConductorId = 2, Activo = true });
        var servicio = new UnidadOperativaServicio(new UnidadOperativaRepositorioFalso(), vehiculoRepo, conductorRepo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(1, new CrearUnidadOperativaDto { VehiculoId = 5 }));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoElVehiculoYaTieneUnaUnidadOperativa()
    {
        var conductorRepo = new ConductorRepositorioFalso(new Conductor { ConductorId = 1, UsuarioId = 10, Activo = true });
        var vehiculoRepo = new VehiculoRepositorioFalso(new Vehiculo { VehiculoId = 5, ConductorId = 1, Activo = true });
        var servicio = new UnidadOperativaServicio(new UnidadOperativaRepositorioFalso(), vehiculoRepo, conductorRepo);

        await servicio.CrearAsync(1, new CrearUnidadOperativaDto { VehiculoId = 5 });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(1, new CrearUnidadOperativaDto { VehiculoId = 5 }));
    }

    [Fact]
    public async Task DesactivarAsync_Desactiva_CuandoPerteneceAlConductor()
    {
        var conductorRepo = new ConductorRepositorioFalso(new Conductor { ConductorId = 1, UsuarioId = 10, Activo = true });
        var vehiculoRepo = new VehiculoRepositorioFalso(new Vehiculo { VehiculoId = 5, ConductorId = 1, Activo = true });
        var servicio = new UnidadOperativaServicio(new UnidadOperativaRepositorioFalso(), vehiculoRepo, conductorRepo);
        var unidad = await servicio.CrearAsync(1, new CrearUnidadOperativaDto { VehiculoId = 5 });

        await servicio.DesactivarAsync(1, unidad.UnidadOperativaId);
        var unidades = await servicio.ObtenerPorConductorAsync(1);

        Assert.False(unidades.Single().Activa);
    }
}
