using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Exceptions;
using TransportApp.Domain.Rules;

namespace TransportApp.Application.Validators;

/// <summary>
/// Comprueba que un conjunto de barrios pueda convivir en una misma zona (y en
/// el corredor vial de esa zona) sin violar ninguna <see cref="BarreraGeografica"/>
/// declarada por la empresa. Lo comparten la edición de zonas y su
/// reorganización (mover barrios, unir zonas). No modifica ni guarda zonas.
/// </summary>
public class ValidadorBarrerasZona
{
    private readonly IZonaRepositorio _zonaRepositorio;
    private readonly IBarreraGeograficaRepositorio _barreraGeograficaRepositorio;

    /// <summary>Crea el validador con los repositorios que consulta.</summary>
    public ValidadorBarrerasZona(IZonaRepositorio zonaRepositorio, IBarreraGeograficaRepositorio barreraGeograficaRepositorio)
    {
        _zonaRepositorio = zonaRepositorio;
        _barreraGeograficaRepositorio = barreraGeograficaRepositorio;
    }

    /// <summary>
    /// Impide guardar una zona que combine dos barrios entre los que la empresa
    /// declaró una <see cref="BarreraGeografica"/> (por ejemplo, separados por
    /// una barrera topográfica sin vía de conexión útil).
    /// </summary>
    public async Task ValidarSinBarrerasAsync(int empresaId, List<string> barrios)
    {
        if (barrios.Count < 2)
        {
            return;
        }

        var barreras = await _barreraGeograficaRepositorio.ObtenerPorEmpresaAsync(empresaId);
        var par = ReglasBarrerasGeograficas.BuscarParConBarrera(barrios, barreras);
        if (par is not null)
        {
            throw new BarreraGeograficaConflictoException(
                $"No puedes juntar \"{par.Value.BarrioA}\" y \"{par.Value.BarrioB}\" en la misma zona: hay una barrera geográfica declarada entre ellos.");
        }
    }

    /// <summary>
    /// Impide asignar una zona a un <see cref="CorredorVial"/> si alguno de
    /// sus barrios tiene una barrera geográfica declarada con algún barrio de
    /// otra zona que ya esté en ese mismo corredor (un corredor agrupa zonas
    /// para que compartan vehículo, así que nunca puede mezclar barrios que
    /// no tienen conexión real entre sí). <paramref name="zonasExcluidas"/>
    /// son las zonas que no deben contarse como "otras" (la propia zona, o
    /// una que está a punto de eliminarse).
    /// </summary>
    public async Task ValidarCorredorSinBarrerasAsync(int empresaId, int? corredorVialId, List<string> barrios, params int[] zonasExcluidas)
    {
        if (corredorVialId is null)
        {
            return;
        }

        var zonasDelCorredor = (await _zonaRepositorio.ObtenerPorEmpresaAsync(empresaId))
            .Where(z => z.CorredorVialId == corredorVialId && !zonasExcluidas.Contains(z.ZonaId))
            .ToList();

        if (zonasDelCorredor.Count == 0)
        {
            return;
        }

        var todosLosBarrios = new List<string>(barrios);
        foreach (var zonaDelCorredor in zonasDelCorredor)
        {
            todosLosBarrios.AddRange(zonaDelCorredor.Barrios);
        }

        var barreras = await _barreraGeograficaRepositorio.ObtenerPorEmpresaAsync(empresaId);
        var par = ReglasBarrerasGeograficas.BuscarParConBarrera(todosLosBarrios, barreras);
        if (par is not null)
        {
            throw new BarreraGeograficaConflictoException(
                $"No puedes poner esta zona en ese corredor vial: hay una barrera geográfica declarada entre \"{par.Value.BarrioA}\" y \"{par.Value.BarrioB}\".");
        }
    }
}
