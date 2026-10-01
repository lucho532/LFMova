using TransportApp.Application.DTOs.Zonas;
using TransportApp.Application.Implementations;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;

namespace TransportApp.UnitTests.Application.Implementations;

public class BarreraGeograficaServicioTests
{
    private class BarreraGeograficaRepositorioFalso : IBarreraGeograficaRepositorio
    {
        private readonly Dictionary<int, BarreraGeografica> _barreras = new();
        private int _siguienteId = 1;

        public Task<BarreraGeografica?> ObtenerPorIdAsync(int barreraGeograficaId)
            => Task.FromResult(_barreras.TryGetValue(barreraGeograficaId, out var b) ? b : null);

        public Task<List<BarreraGeografica>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_barreras.Values.Where(b => b.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(BarreraGeografica barrera)
        {
            barrera.BarreraGeograficaId = _siguienteId++;
            _barreras[barrera.BarreraGeograficaId] = barrera;
            return Task.CompletedTask;
        }

        public void Eliminar(BarreraGeografica barrera) => _barreras.Remove(barrera.BarreraGeograficaId);

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task CrearAsync_AsignaLaEmpresaYRecortaLosBarrios()
    {
        var servicio = new BarreraGeograficaServicio(new BarreraGeograficaRepositorioFalso());

        var barrera = await servicio.CrearAsync(1, new CrearBarreraGeograficaDto { BarrioA = " La Linda ", BarrioB = " La Francia " });

        Assert.Equal(1, barrera.EmpresaId);
        Assert.Equal("La Linda", barrera.BarrioA);
        Assert.Equal("La Francia", barrera.BarrioB);
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoAlgunBarrioEstaVacio()
    {
        var servicio = new BarreraGeograficaServicio(new BarreraGeograficaRepositorioFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(1, new CrearBarreraGeograficaDto { BarrioA = "La Linda", BarrioB = "  " }));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoLosDosBarriosSonIguales()
    {
        var servicio = new BarreraGeograficaServicio(new BarreraGeograficaRepositorioFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(1, new CrearBarreraGeograficaDto { BarrioA = "La Linda", BarrioB = "la linda" }));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoYaExisteLaBarreraEntreEseParDeBarrios()
    {
        var repositorio = new BarreraGeograficaRepositorioFalso();
        var servicio = new BarreraGeograficaServicio(repositorio);
        await servicio.CrearAsync(1, new CrearBarreraGeograficaDto { BarrioA = "La Linda", BarrioB = "La Francia" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(1, new CrearBarreraGeograficaDto { BarrioA = "La Francia", BarrioB = "La Linda" }));
    }

    [Fact]
    public async Task EliminarAsync_LanzaExcepcion_CuandoLaBarreraEsDeOtraEmpresa()
    {
        var repositorio = new BarreraGeograficaRepositorioFalso();
        var servicio = new BarreraGeograficaServicio(repositorio);
        var barrera = await servicio.CrearAsync(1, new CrearBarreraGeograficaDto { BarrioA = "La Linda", BarrioB = "La Francia" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.EliminarAsync(2, barrera.BarreraGeograficaId));
    }

    [Fact]
    public async Task EliminarAsync_EliminaLaBarrera_CuandoPerteneceALaEmpresa()
    {
        var repositorio = new BarreraGeograficaRepositorioFalso();
        var servicio = new BarreraGeograficaServicio(repositorio);
        var barrera = await servicio.CrearAsync(1, new CrearBarreraGeograficaDto { BarrioA = "La Linda", BarrioB = "La Francia" });

        await servicio.EliminarAsync(1, barrera.BarreraGeograficaId);
        var lista = await servicio.ObtenerPorEmpresaAsync(1);

        Assert.Empty(lista);
    }
}
