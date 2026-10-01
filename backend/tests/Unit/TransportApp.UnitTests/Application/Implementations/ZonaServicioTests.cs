using TransportApp.Application.DTOs.Zonas;
using TransportApp.Application.Implementations;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Exceptions;

namespace TransportApp.UnitTests.Application.Implementations;

public class ZonaServicioTests
{
    private class ZonaRepositorioFalso : IZonaRepositorio
    {
        private readonly Dictionary<int, Zona> _zonas = new();
        private int _siguienteId = 1;

        public Task<Zona?> ObtenerPorIdAsync(int zonaId)
            => Task.FromResult(_zonas.TryGetValue(zonaId, out var z) ? z : null);

        public Task<List<Zona>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_zonas.Values.Where(z => z.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(Zona zona)
        {
            zona.ZonaId = _siguienteId++;
            _zonas[zona.ZonaId] = zona;
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class BarreraGeograficaRepositorioFalso : IBarreraGeograficaRepositorio
    {
        private readonly List<BarreraGeografica> _barreras = new();
        private int _siguienteId = 1;

        public BarreraGeograficaRepositorioFalso ConBarrera(int empresaId, string barrioA, string barrioB)
        {
            _barreras.Add(new BarreraGeografica { BarreraGeograficaId = _siguienteId++, EmpresaId = empresaId, BarrioA = barrioA, BarrioB = barrioB });
            return this;
        }

        public Task<BarreraGeografica?> ObtenerPorIdAsync(int barreraGeograficaId)
            => Task.FromResult(_barreras.FirstOrDefault(b => b.BarreraGeograficaId == barreraGeograficaId));

        public Task<List<BarreraGeografica>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_barreras.Where(b => b.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(BarreraGeografica barrera)
        {
            barrera.BarreraGeograficaId = _siguienteId++;
            _barreras.Add(barrera);
            return Task.CompletedTask;
        }

        public void Eliminar(BarreraGeografica barrera) => _barreras.Remove(barrera);

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private static ZonaServicio CrearServicio(IZonaRepositorio? zonaRepositorio = null, IBarreraGeograficaRepositorio? barreraRepositorio = null)
        => new(zonaRepositorio ?? new ZonaRepositorioFalso(), barreraRepositorio ?? new BarreraGeograficaRepositorioFalso());

    [Fact]
    public async Task CrearAsync_AsignaLaEmpresaYQuedaActiva()
    {
        var servicio = CrearServicio();

        var zona = await servicio.CrearAsync(1, new CrearZonaDto { Nombre = "Zona Norte", Barrios = new List<string> { "La Enea", "Palermo" } });

        Assert.Equal(1, zona.EmpresaId);
        Assert.True(zona.Activa);
        Assert.Equal(new[] { "La Enea", "Palermo" }, zona.Barrios);
    }

    [Fact]
    public async Task CrearAsync_QuitaEspaciosYBarriosRepetidosOVacios()
    {
        var servicio = CrearServicio();

        var zona = await servicio.CrearAsync(1, new CrearZonaDto { Nombre = "Zona Norte", Barrios = new List<string> { " La Enea ", "la enea", "", "  ", "Palermo" } });

        Assert.Equal(new[] { "La Enea", "Palermo" }, zona.Barrios);
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoElNombreEstaVacio()
    {
        var servicio = CrearServicio();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(1, new CrearZonaDto { Nombre = "  ", Barrios = new List<string>() }));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoLosBarriosTienenUnaBarreraGeograficaDeclarada()
    {
        var barreras = new BarreraGeograficaRepositorioFalso().ConBarrera(1, "La Linda", "La Francia");
        var servicio = CrearServicio(barreraRepositorio: barreras);

        await Assert.ThrowsAsync<BarreraGeograficaConflictoException>(() =>
            servicio.CrearAsync(1, new CrearZonaDto { Nombre = "Atardeceres", Barrios = new List<string> { "La Linda", "La Francia" } }));
    }

    [Fact]
    public async Task ActualizarAsync_LanzaExcepcion_CuandoLosBarriosTienenUnaBarreraGeograficaDeclarada()
    {
        var zonaRepositorio = new ZonaRepositorioFalso();
        var barreras = new BarreraGeograficaRepositorioFalso();
        var servicio = CrearServicio(zonaRepositorio, barreras);
        var zona = await servicio.CrearAsync(1, new CrearZonaDto { Nombre = "Atardeceres", Barrios = new List<string> { "La Linda" } });
        barreras.ConBarrera(1, "La Linda", "La Francia");

        await Assert.ThrowsAsync<BarreraGeograficaConflictoException>(() =>
            servicio.ActualizarAsync(1, zona.ZonaId, new ActualizarZonaDto { Nombre = "Atardeceres", Barrios = new List<string> { "La Linda", "La Francia" } }));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoElCorredorYaTieneUnaZonaConBarreraContraEsta()
    {
        var zonaRepositorio = new ZonaRepositorioFalso();
        var barreras = new BarreraGeograficaRepositorioFalso().ConBarrera(1, "La Linda", "La Francia");
        var servicio = CrearServicio(zonaRepositorio, barreras);
        var zonaFrancia = await servicio.CrearAsync(1, new CrearZonaDto { Nombre = "AT-S06 Oriente", Barrios = new List<string> { "La Francia" }, CorredorVialId = 99 });

        // La Linda no tiene barrera con nada dentro de SU propia zona, pero el corredor 99 ya tiene
        // una zona con La Francia, y La Linda <-> La Francia sí tiene barrera declarada.
        await Assert.ThrowsAsync<BarreraGeograficaConflictoException>(() =>
            servicio.CrearAsync(1, new CrearZonaDto { Nombre = "AT-S01 Occidente", Barrios = new List<string> { "La Linda" }, CorredorVialId = 99 }));

        Assert.NotNull(zonaFrancia);
    }

    [Fact]
    public async Task CrearAsync_PermiteElCorredor_CuandoLasZonasNoTienenBarreraEntreSi()
    {
        var zonaRepositorio = new ZonaRepositorioFalso();
        var servicio = CrearServicio(zonaRepositorio);
        await servicio.CrearAsync(1, new CrearZonaDto { Nombre = "AT-S02 Villa Pilar", Barrios = new List<string> { "Villa Pilar" }, CorredorVialId = 99 });

        var zona = await servicio.CrearAsync(1, new CrearZonaDto { Nombre = "AT-S01 Occidente", Barrios = new List<string> { "La Linda" }, CorredorVialId = 99 });

        Assert.Equal(99, zona.CorredorVialId);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_DevuelveNulo_CuandoLaZonaEsDeOtraEmpresa()
    {
        var repositorio = new ZonaRepositorioFalso();
        var servicio = CrearServicio(repositorio);
        var zona = await servicio.CrearAsync(1, new CrearZonaDto { Nombre = "Zona Norte", Barrios = new List<string>() });

        var resultado = await servicio.ObtenerPorIdAsync(2, zona.ZonaId);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ActualizarAsync_LanzaExcepcion_CuandoLaZonaEsDeOtraEmpresa()
    {
        var repositorio = new ZonaRepositorioFalso();
        var servicio = CrearServicio(repositorio);
        var zona = await servicio.CrearAsync(1, new CrearZonaDto { Nombre = "Zona Norte", Barrios = new List<string>() });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.ActualizarAsync(2, zona.ZonaId, new ActualizarZonaDto { Nombre = "Otra", Barrios = new List<string>() }));
    }

    [Fact]
    public async Task AgregarBarrioAsync_AgregaElBarrioComoAliasNuevo()
    {
        var repositorio = new ZonaRepositorioFalso();
        var servicio = CrearServicio(repositorio);
        var zona = await servicio.CrearAsync(1, new CrearZonaDto { Nombre = "Zona Norte", Barrios = new List<string> { "La Enea" } });

        var actualizada = await servicio.AgregarBarrioAsync(1, zona.ZonaId, "Barrio Nuevo");

        Assert.Equal(new[] { "La Enea", "Barrio Nuevo" }, actualizada.Barrios);
    }

    [Fact]
    public async Task AgregarBarrioAsync_NoDuplica_CuandoElBarrioYaEstaba()
    {
        var repositorio = new ZonaRepositorioFalso();
        var servicio = CrearServicio(repositorio);
        var zona = await servicio.CrearAsync(1, new CrearZonaDto { Nombre = "Zona Norte", Barrios = new List<string> { "La Enea" } });

        var actualizada = await servicio.AgregarBarrioAsync(1, zona.ZonaId, "la enea");

        Assert.Equal(new[] { "La Enea" }, actualizada.Barrios);
    }

    [Fact]
    public async Task AgregarBarrioAsync_LanzaExcepcion_CuandoElBarrioEstaVacio()
    {
        var repositorio = new ZonaRepositorioFalso();
        var servicio = CrearServicio(repositorio);
        var zona = await servicio.CrearAsync(1, new CrearZonaDto { Nombre = "Zona Norte", Barrios = new List<string>() });

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.AgregarBarrioAsync(1, zona.ZonaId, "  "));
    }

    [Fact]
    public async Task AgregarBarrioAsync_LanzaExcepcion_CuandoLaZonaEsDeOtraEmpresa()
    {
        var repositorio = new ZonaRepositorioFalso();
        var servicio = CrearServicio(repositorio);
        var zona = await servicio.CrearAsync(1, new CrearZonaDto { Nombre = "Zona Norte", Barrios = new List<string>() });

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.AgregarBarrioAsync(2, zona.ZonaId, "Barrio Nuevo"));
    }

    [Fact]
    public async Task AgregarBarrioAsync_LanzaExcepcion_CuandoElNuevoBarrioTieneBarreraConOtroDeLaZona()
    {
        var zonaRepositorio = new ZonaRepositorioFalso();
        var barreras = new BarreraGeograficaRepositorioFalso().ConBarrera(1, "La Linda", "La Francia");
        var servicio = CrearServicio(zonaRepositorio, barreras);
        var zona = await servicio.CrearAsync(1, new CrearZonaDto { Nombre = "Atardeceres", Barrios = new List<string> { "La Linda" } });

        await Assert.ThrowsAsync<BarreraGeograficaConflictoException>(() => servicio.AgregarBarrioAsync(1, zona.ZonaId, "La Francia"));
    }

    [Fact]
    public async Task DesactivarAsync_DesactivaLaZona_CuandoPerteneceALaEmpresa()
    {
        var repositorio = new ZonaRepositorioFalso();
        var servicio = CrearServicio(repositorio);
        var zona = await servicio.CrearAsync(1, new CrearZonaDto { Nombre = "Zona Norte", Barrios = new List<string>() });

        await servicio.DesactivarAsync(1, zona.ZonaId);
        var actualizada = await servicio.ObtenerPorIdAsync(1, zona.ZonaId);

        Assert.False(actualizada!.Activa);
    }
}
