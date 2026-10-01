using TransportApp.Application.Interfaces;
using TransportApp.Application.Validators;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Rules;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa la reorganización de las zonas registradas de una empresa (mover
/// barrios entre zonas, unir zonas y eliminarlas). Aplica
/// <see cref="ReglasMultiempresa"/> para que solo se toquen zonas de la propia
/// empresa y respeta las barreras geográficas declaradas. No accede
/// directamente a Entity Framework Core.
/// </summary>
public class ZonaReorganizacionServicio : IZonaReorganizacionServicio
{
    private readonly IZonaRepositorio _zonaRepositorio;
    private readonly ValidadorBarrerasZona _validadorBarreras;

    /// <summary>Crea el servicio con sus repositorios.</summary>
    public ZonaReorganizacionServicio(IZonaRepositorio zonaRepositorio, IBarreraGeograficaRepositorio barreraGeograficaRepositorio)
    {
        _zonaRepositorio = zonaRepositorio;
        _validadorBarreras = new ValidadorBarrerasZona(zonaRepositorio, barreraGeograficaRepositorio);
    }

    /// <inheritdoc />
    public async Task MoverBarrioAsync(int empresaId, int zonaOrigenId, string barrio, int zonaDestinoId)
    {
        var (origen, destino) = await ObtenerParDeZonasOFallarAsync(empresaId, zonaOrigenId, zonaDestinoId);

        var barrioEnOrigen = origen.Barrios.FirstOrDefault(b => string.Equals(b, barrio?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (barrioEnOrigen is null)
        {
            throw new InvalidOperationException("El barrio indicado no pertenece a la zona de origen.");
        }

        var quedaVacia = origen.Barrios.Count == 1;
        await AgregarBarriosAsync(empresaId, destino, new List<string> { barrioEnOrigen }, quedaVacia ? origen.ZonaId : null);

        // Se asigna una lista nueva (no se muta la existente) para que el cambio de la columna se detecte al guardar.
        origen.Barrios = origen.Barrios.Where(b => b != barrioEnOrigen).ToList();
        if (quedaVacia)
        {
            _zonaRepositorio.Eliminar(origen);
        }

        await _zonaRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task UnirAsync(int empresaId, int zonaOrigenId, int zonaDestinoId)
    {
        var (origen, destino) = await ObtenerParDeZonasOFallarAsync(empresaId, zonaOrigenId, zonaDestinoId);

        await AgregarBarriosAsync(empresaId, destino, origen.Barrios, origen.ZonaId);
        _zonaRepositorio.Eliminar(origen);

        await _zonaRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task EliminarAsync(int empresaId, int zonaId)
    {
        var zona = await ObtenerZonaDeLaEmpresaOFallarAsync(empresaId, zonaId);
        _zonaRepositorio.Eliminar(zona);
        await _zonaRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task ReordenarAsync(int empresaId, List<int> zonaIds)
    {
        var zonas = await _zonaRepositorio.ObtenerPorEmpresaAsync(empresaId);
        var posicion = zonaIds.Distinct().Select((zonaId, indice) => (zonaId, indice)).ToDictionary(p => p.zonaId, p => p.indice);

        var ordenadas = zonas
            .OrderBy(z => posicion.TryGetValue(z.ZonaId, out var indice) ? indice : int.MaxValue)
            .ThenBy(z => z.Orden)
            .ThenBy(z => z.ZonaId)
            .ToList();

        for (var indice = 0; indice < ordenadas.Count; indice++)
        {
            ordenadas[indice].Orden = indice;
        }

        await _zonaRepositorio.GuardarCambiosAsync();
    }

    /// <summary>
    /// Suma barrios a la zona de destino (sin repetir los que ya tiene) tras comprobar que no chocan con
    /// ninguna barrera geográfica. <paramref name="zonaQueSeElimina"/> es la zona de origen cuando va a
    /// desaparecer: no debe contarse como "otra zona" de su corredor.
    /// </summary>
    private async Task AgregarBarriosAsync(int empresaId, Zona destino, List<string> barrios, int? zonaQueSeElimina)
    {
        var barriosDestino = destino.Barrios
            .Concat(barrios)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var zonasExcluidas = zonaQueSeElimina is null
            ? new[] { destino.ZonaId }
            : new[] { destino.ZonaId, zonaQueSeElimina.Value };

        await _validadorBarreras.ValidarSinBarrerasAsync(empresaId, barriosDestino);
        await _validadorBarreras.ValidarCorredorSinBarrerasAsync(empresaId, destino.CorredorVialId, barriosDestino, zonasExcluidas);

        destino.Barrios = barriosDestino;
    }

    private async Task<(Zona Origen, Zona Destino)> ObtenerParDeZonasOFallarAsync(int empresaId, int zonaOrigenId, int zonaDestinoId)
    {
        if (zonaOrigenId == zonaDestinoId)
        {
            throw new InvalidOperationException("La zona de origen y la de destino son la misma.");
        }

        var origen = await ObtenerZonaDeLaEmpresaOFallarAsync(empresaId, zonaOrigenId);
        var destino = await ObtenerZonaDeLaEmpresaOFallarAsync(empresaId, zonaDestinoId);
        return (origen, destino);
    }

    private async Task<Zona> ObtenerZonaDeLaEmpresaOFallarAsync(int empresaId, int zonaId)
    {
        var zona = await _zonaRepositorio.ObtenerPorIdAsync(zonaId);
        if (zona is null || !ReglasMultiempresa.ZonaPerteneceAEmpresa(zona, empresaId))
        {
            throw new InvalidOperationException("La zona indicada no existe en esta empresa.");
        }

        return zona;
    }
}
