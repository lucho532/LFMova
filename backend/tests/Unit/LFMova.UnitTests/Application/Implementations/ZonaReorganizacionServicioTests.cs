using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Exceptions;

namespace LFMova.UnitTests.Application.Implementations;

public class ZonaReorganizacionServicioTests
{
    private class ZonaRepositorioFalso : IZonaRepositorio
    {
        private readonly List<Zona> _zonas;

        public ZonaRepositorioFalso(params Zona[] zonas) => _zonas = zonas.ToList();

        public Task<Zona?> ObtenerPorIdAsync(int zonaId) => Task.FromResult(_zonas.FirstOrDefault(z => z.ZonaId == zonaId));

        public Task<List<Zona>> ObtenerPorEmpresaAsync(int empresaId) => Task.FromResult(_zonas.Where(z => z.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(Zona zona)
        {
            _zonas.Add(zona);
            return Task.CompletedTask;
        }

        public void Eliminar(Zona zona) => _zonas.Remove(zona);

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class BarreraGeograficaRepositorioFalso : IBarreraGeograficaRepositorio
    {
        private readonly List<BarreraGeografica> _barreras;

        public BarreraGeograficaRepositorioFalso(params BarreraGeografica[] barreras) => _barreras = barreras.ToList();

        public Task<BarreraGeografica?> ObtenerPorIdAsync(int barreraGeograficaId)
            => Task.FromResult(_barreras.FirstOrDefault(b => b.BarreraGeograficaId == barreraGeograficaId));

        public Task<List<BarreraGeografica>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_barreras.Where(b => b.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(BarreraGeografica barrera)
        {
            _barreras.Add(barrera);
            return Task.CompletedTask;
        }

        public void Eliminar(BarreraGeografica barrera) => _barreras.Remove(barrera);

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private static Zona CrearZona(int zonaId, string nombre, params string[] barrios)
        => new() { ZonaId = zonaId, EmpresaId = 1, Nombre = nombre, Barrios = barrios.ToList(), Activa = true };

    private static ZonaReorganizacionServicio CrearServicio(ZonaRepositorioFalso zonas, params BarreraGeografica[] barreras)
        => new(zonas, new BarreraGeograficaRepositorioFalso(barreras));

    [Fact]
    public async Task MoverBarrioAsync_PasaElBarrioALaZonaDeDestino()
    {
        var origen = CrearZona(1, "Norte", "La Enea", "Palermo");
        var destino = CrearZona(2, "Sur", "Chipre");
        var repositorio = new ZonaRepositorioFalso(origen, destino);

        await CrearServicio(repositorio).MoverBarrioAsync(1, 1, "palermo", 2);

        Assert.Equal(new[] { "La Enea" }, origen.Barrios);
        Assert.Equal(new[] { "Chipre", "Palermo" }, destino.Barrios);
        Assert.Equal(2, (await repositorio.ObtenerPorEmpresaAsync(1)).Count);
    }

    [Fact]
    public async Task MoverBarrioAsync_EliminaLaZonaDeOrigen_CuandoSeQuedaSinBarrios()
    {
        var origen = CrearZona(1, "Palermo", "Palermo");
        var destino = CrearZona(2, "Sur", "Chipre");
        var repositorio = new ZonaRepositorioFalso(origen, destino);

        await CrearServicio(repositorio).MoverBarrioAsync(1, 1, "Palermo", 2);

        var zonas = await repositorio.ObtenerPorEmpresaAsync(1);
        Assert.Equal(destino, Assert.Single(zonas));
        Assert.Equal(new[] { "Chipre", "Palermo" }, destino.Barrios);
    }

    [Fact]
    public async Task MoverBarrioAsync_Falla_CuandoElBarrioNoEstaEnLaZonaDeOrigen()
    {
        var repositorio = new ZonaRepositorioFalso(CrearZona(1, "Norte", "La Enea"), CrearZona(2, "Sur", "Chipre"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CrearServicio(repositorio).MoverBarrioAsync(1, 1, "Palermo", 2));
    }

    [Fact]
    public async Task MoverBarrioAsync_Falla_CuandoHayBarreraConUnBarrioDelDestino()
    {
        var origen = CrearZona(1, "Norte", "La Enea", "Palermo");
        var destino = CrearZona(2, "Sur", "Chipre");
        var repositorio = new ZonaRepositorioFalso(origen, destino);
        var barrera = new BarreraGeografica { BarreraGeograficaId = 1, EmpresaId = 1, BarrioA = "Palermo", BarrioB = "Chipre" };

        await Assert.ThrowsAsync<BarreraGeograficaConflictoException>(() => CrearServicio(repositorio, barrera).MoverBarrioAsync(1, 1, "Palermo", 2));

        Assert.Equal(new[] { "La Enea", "Palermo" }, origen.Barrios);
        Assert.Equal(new[] { "Chipre" }, destino.Barrios);
    }

    [Fact]
    public async Task MoverBarrioAsync_Falla_CuandoLaZonaDeDestinoEsDeOtraEmpresa()
    {
        var destino = CrearZona(2, "Sur", "Chipre");
        destino.EmpresaId = 99;
        var repositorio = new ZonaRepositorioFalso(CrearZona(1, "Norte", "La Enea"), destino);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CrearServicio(repositorio).MoverBarrioAsync(1, 1, "La Enea", 2));
    }

    [Fact]
    public async Task UnirAsync_PasaTodosLosBarriosSinRepetirYEliminaLaZonaDeOrigen()
    {
        var origen = CrearZona(1, "Norte", "La Enea", "chipre");
        var destino = CrearZona(2, "Sur", "Chipre");
        var repositorio = new ZonaRepositorioFalso(origen, destino);

        await CrearServicio(repositorio).UnirAsync(1, 1, 2);

        Assert.Equal(destino, Assert.Single(await repositorio.ObtenerPorEmpresaAsync(1)));
        Assert.Equal(new[] { "Chipre", "La Enea" }, destino.Barrios);
        Assert.Equal("Sur", destino.Nombre);
    }

    [Fact]
    public async Task UnirAsync_Falla_CuandoSeUneUnaZonaConsigoMisma()
    {
        var repositorio = new ZonaRepositorioFalso(CrearZona(1, "Norte", "La Enea"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CrearServicio(repositorio).UnirAsync(1, 1, 1));
    }

    [Fact]
    public async Task UnirAsync_NoCuentaLaZonaDeOrigenComoOtraZonaDelCorredor()
    {
        var origen = CrearZona(1, "Norte", "La Enea");
        var destino = CrearZona(2, "Sur", "Chipre");
        origen.CorredorVialId = 7;
        destino.CorredorVialId = 7;
        var repositorio = new ZonaRepositorioFalso(origen, destino);

        await CrearServicio(repositorio).UnirAsync(1, 1, 2);

        Assert.Equal(new[] { "Chipre", "La Enea" }, destino.Barrios);
    }

    [Fact]
    public async Task ReordenarAsync_GuardaLaPosicionYDejaAlFinalLasZonasQueNoVienen()
    {
        var norte = CrearZona(1, "Norte", "La Enea");
        var sur = CrearZona(2, "Sur", "Chipre");
        var centro = CrearZona(3, "Centro", "Centro");
        var ajena = CrearZona(4, "Ajena", "Otro");
        ajena.EmpresaId = 99;
        var repositorio = new ZonaRepositorioFalso(norte, sur, centro, ajena);

        await CrearServicio(repositorio).ReordenarAsync(1, new List<int> { 3, 1, 4 });

        Assert.Equal(0, centro.Orden);
        Assert.Equal(1, norte.Orden);
        Assert.Equal(2, sur.Orden);
        Assert.Equal(0, ajena.Orden);
    }

    [Fact]
    public async Task EliminarAsync_QuitaLaZona()
    {
        var repositorio = new ZonaRepositorioFalso(CrearZona(1, "Norte", "La Enea"), CrearZona(2, "Sur", "Chipre"));

        await CrearServicio(repositorio).EliminarAsync(1, 1);

        Assert.Equal(2, Assert.Single(await repositorio.ObtenerPorEmpresaAsync(1)).ZonaId);
    }

    [Fact]
    public async Task EliminarAsync_Falla_CuandoLaZonaEsDeOtraEmpresa()
    {
        var repositorio = new ZonaRepositorioFalso(CrearZona(1, "Norte", "La Enea"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CrearServicio(repositorio).EliminarAsync(99, 1));
    }
}
