using LFMova.Application.DTOs.Zonas;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;

namespace LFMova.UnitTests.Application.Implementations;

public class MacroZonaServicioTests
{
    private class MacroZonaRepositorioFalso : IMacroZonaRepositorio
    {
        private readonly Dictionary<int, MacroZona> _macroZonas = new();
        private int _siguienteId = 1;

        public Task<MacroZona?> ObtenerPorIdAsync(int macroZonaId)
            => Task.FromResult(_macroZonas.TryGetValue(macroZonaId, out var m) ? m : null);

        public Task<List<MacroZona>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_macroZonas.Values.Where(m => m.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(MacroZona macroZona)
        {
            macroZona.MacroZonaId = _siguienteId++;
            _macroZonas[macroZona.MacroZonaId] = macroZona;
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task CrearAsync_AsignaLaEmpresaYQuedaActiva()
    {
        var servicio = new MacroZonaServicio(new MacroZonaRepositorioFalso());

        var macroZona = await servicio.CrearAsync(1, new CrearMacroZonaDto { Nombre = "Atardeceres" });

        Assert.Equal(1, macroZona.EmpresaId);
        Assert.True(macroZona.Activa);
        Assert.Equal("Atardeceres", macroZona.Nombre);
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoElNombreEstaVacio()
    {
        var servicio = new MacroZonaServicio(new MacroZonaRepositorioFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(1, new CrearMacroZonaDto { Nombre = "  " }));
    }

    [Fact]
    public async Task DesactivarAsync_DesactivaLaMacroZona_CuandoPerteneceALaEmpresa()
    {
        var repositorio = new MacroZonaRepositorioFalso();
        var servicio = new MacroZonaServicio(repositorio);
        var macroZona = await servicio.CrearAsync(1, new CrearMacroZonaDto { Nombre = "Atardeceres" });

        await servicio.DesactivarAsync(1, macroZona.MacroZonaId);
        var lista = await servicio.ObtenerPorEmpresaAsync(1);

        Assert.False(lista.Single().Activa);
    }
}
