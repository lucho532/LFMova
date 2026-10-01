using TransportApp.Application.DTOs.Zonas;
using TransportApp.Application.Implementations;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;

namespace TransportApp.UnitTests.Application.Implementations;

public class CorredorVialServicioTests
{
    private class CorredorVialRepositorioFalso : ICorredorVialRepositorio
    {
        private readonly Dictionary<int, CorredorVial> _corredores = new();
        private int _siguienteId = 1;

        public Task<CorredorVial?> ObtenerPorIdAsync(int corredorVialId)
            => Task.FromResult(_corredores.TryGetValue(corredorVialId, out var c) ? c : null);

        public Task<List<CorredorVial>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_corredores.Values.Where(c => c.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(CorredorVial corredorVial)
        {
            corredorVial.CorredorVialId = _siguienteId++;
            _corredores[corredorVial.CorredorVialId] = corredorVial;
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task CrearAsync_AsignaLaEmpresaYQuedaActivo()
    {
        var servicio = new CorredorVialServicio(new CorredorVialRepositorioFalso());

        var corredor = await servicio.CrearAsync(1, new CrearCorredorVialDto { Nombre = "Atardeceres Occidente" });

        Assert.Equal(1, corredor.EmpresaId);
        Assert.True(corredor.Activo);
        Assert.Equal("Atardeceres Occidente", corredor.Nombre);
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoElNombreEstaVacio()
    {
        var servicio = new CorredorVialServicio(new CorredorVialRepositorioFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(1, new CrearCorredorVialDto { Nombre = "  " }));
    }

    [Fact]
    public async Task ActualizarAsync_LanzaExcepcion_CuandoElCorredorEsDeOtraEmpresa()
    {
        var repositorio = new CorredorVialRepositorioFalso();
        var servicio = new CorredorVialServicio(repositorio);
        var corredor = await servicio.CrearAsync(1, new CrearCorredorVialDto { Nombre = "Atardeceres Occidente" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.ActualizarAsync(2, corredor.CorredorVialId, new ActualizarCorredorVialDto { Nombre = "Otro" }));
    }

    [Fact]
    public async Task DesactivarAsync_DesactivaElCorredor_CuandoPerteneceALaEmpresa()
    {
        var repositorio = new CorredorVialRepositorioFalso();
        var servicio = new CorredorVialServicio(repositorio);
        var corredor = await servicio.CrearAsync(1, new CrearCorredorVialDto { Nombre = "Atardeceres Occidente" });

        await servicio.DesactivarAsync(1, corredor.CorredorVialId);
        var lista = await servicio.ObtenerPorEmpresaAsync(1);

        Assert.False(lista.Single().Activo);
    }
}
