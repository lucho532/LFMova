using LFMova.Application.DTOs.Sedes;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;

namespace LFMova.UnitTests.Application.Implementations;

public class SedeServicioTests
{
    private class SedeRepositorioFalso : ISedeRepositorio
    {
        private readonly Dictionary<int, Sede> _sedes = new();
        private int _siguienteId = 1;

        public Task<Sede?> ObtenerPorIdAsync(int sedeId)
            => Task.FromResult(_sedes.TryGetValue(sedeId, out var s) ? s : null);

        public Task<List<Sede>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_sedes.Values.Where(s => s.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(Sede sede)
        {
            sede.SedeId = _siguienteId++;
            _sedes[sede.SedeId] = sede;
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task CrearAsync_AsignaLaEmpresaYQuedaActiva()
    {
        var servicio = new SedeServicio(new SedeRepositorioFalso());

        var sede = await servicio.CrearAsync(1, new CrearSedeDto { Nombre = "Sede Norte", Direccion = "Calle 1" });

        Assert.Equal(1, sede.EmpresaId);
        Assert.True(sede.Activa);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_DevuelveNulo_CuandoLaSedeEsDeOtraEmpresa()
    {
        var repositorio = new SedeRepositorioFalso();
        var servicio = new SedeServicio(repositorio);
        var sede = await servicio.CrearAsync(1, new CrearSedeDto { Nombre = "Sede Norte", Direccion = "Calle 1" });

        var resultado = await servicio.ObtenerPorIdAsync(2, sede.SedeId);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ActualizarAsync_LanzaExcepcion_CuandoLaSedeEsDeOtraEmpresa()
    {
        var repositorio = new SedeRepositorioFalso();
        var servicio = new SedeServicio(repositorio);
        var sede = await servicio.CrearAsync(1, new CrearSedeDto { Nombre = "Sede Norte", Direccion = "Calle 1" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.ActualizarAsync(2, sede.SedeId, new ActualizarSedeDto { Nombre = "Otra", Direccion = "Otra" }));
    }

    [Fact]
    public async Task DesactivarAsync_DesactivaLaSede_CuandoPerteneceALaEmpresa()
    {
        var repositorio = new SedeRepositorioFalso();
        var servicio = new SedeServicio(repositorio);
        var sede = await servicio.CrearAsync(1, new CrearSedeDto { Nombre = "Sede Norte", Direccion = "Calle 1" });

        await servicio.DesactivarAsync(1, sede.SedeId);
        var actualizada = await servicio.ObtenerPorIdAsync(1, sede.SedeId);

        Assert.False(actualizada!.Activa);
    }
}
