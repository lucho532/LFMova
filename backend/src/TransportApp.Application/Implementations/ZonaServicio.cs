using TransportApp.Application.DTOs.Zonas;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Mappers;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Exceptions;
using TransportApp.Domain.Rules;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de administración de zonas geográficas de
/// recogida de una empresa. Aplica <see cref="ReglasMultiempresa"/> para
/// garantizar que una zona solamente se consulte o modifique dentro del
/// contexto de su propia empresa. No accede directamente a Entity Framework
/// Core.
/// </summary>
public class ZonaServicio : IZonaServicio
{
    private readonly IZonaRepositorio _zonaRepositorio;
    private readonly IBarreraGeograficaRepositorio _barreraGeograficaRepositorio;

    /// <summary>Crea el servicio con su repositorio.</summary>
    public ZonaServicio(IZonaRepositorio zonaRepositorio, IBarreraGeograficaRepositorio barreraGeograficaRepositorio)
    {
        _zonaRepositorio = zonaRepositorio;
        _barreraGeograficaRepositorio = barreraGeograficaRepositorio;
    }

    /// <inheritdoc />
    public async Task<ZonaDto> CrearAsync(int empresaId, CrearZonaDto datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Nombre))
        {
            throw new InvalidOperationException("El nombre de la zona es obligatorio.");
        }

        var barrios = LimpiarBarrios(datos.Barrios);
        await ValidarSinBarrerasAsync(empresaId, barrios);
        await ValidarCorredorSinBarrerasAsync(empresaId, datos.CorredorVialId, barrios, zonaIdActual: null);

        var zona = new Zona
        {
            EmpresaId = empresaId,
            Nombre = datos.Nombre.Trim(),
            Barrios = barrios,
            MacroZonaId = datos.MacroZonaId,
            CorredorVialId = datos.CorredorVialId,
            Activa = true
        };

        await _zonaRepositorio.AgregarAsync(zona);
        await _zonaRepositorio.GuardarCambiosAsync();

        // Se vuelve a consultar para traer el nombre de la MacroZona (si se asignó una),
        // ya que la entidad recién creada no trae cargada esa relación en memoria.
        var zonaCreada = await _zonaRepositorio.ObtenerPorIdAsync(zona.ZonaId);
        return ZonaMapper.AZonaDto(zonaCreada!);
    }

    /// <inheritdoc />
    public async Task<ZonaDto?> ObtenerPorIdAsync(int empresaId, int zonaId)
    {
        var zona = await _zonaRepositorio.ObtenerPorIdAsync(zonaId);
        if (zona is null || !ReglasMultiempresa.ZonaPerteneceAEmpresa(zona, empresaId))
        {
            return null;
        }

        return ZonaMapper.AZonaDto(zona);
    }

    /// <inheritdoc />
    public async Task<List<ZonaDto>> ObtenerPorEmpresaAsync(int empresaId)
    {
        var zonas = await _zonaRepositorio.ObtenerPorEmpresaAsync(empresaId);
        return zonas.Select(ZonaMapper.AZonaDto).ToList();
    }

    /// <inheritdoc />
    public async Task ActualizarAsync(int empresaId, int zonaId, ActualizarZonaDto datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Nombre))
        {
            throw new InvalidOperationException("El nombre de la zona es obligatorio.");
        }

        var zona = await ObtenerZonaDeLaEmpresaOFallarAsync(empresaId, zonaId);
        var barrios = LimpiarBarrios(datos.Barrios);
        await ValidarSinBarrerasAsync(empresaId, barrios);
        await ValidarCorredorSinBarrerasAsync(empresaId, datos.CorredorVialId, barrios, zonaIdActual: zonaId);

        zona.Nombre = datos.Nombre.Trim();
        zona.Barrios = barrios;
        zona.MacroZonaId = datos.MacroZonaId;
        zona.CorredorVialId = datos.CorredorVialId;

        await _zonaRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task<ZonaDto> AgregarBarrioAsync(int empresaId, int zonaId, string barrio)
    {
        if (string.IsNullOrWhiteSpace(barrio))
        {
            throw new InvalidOperationException("El barrio es obligatorio.");
        }

        var zona = await ObtenerZonaDeLaEmpresaOFallarAsync(empresaId, zonaId);
        var barrioLimpio = barrio.Trim();
        if (zona.Barrios.Any(b => string.Equals(b, barrioLimpio, StringComparison.OrdinalIgnoreCase)))
        {
            return ZonaMapper.AZonaDto(zona);
        }

        var barriosNuevos = new List<string>(zona.Barrios) { barrioLimpio };
        await ValidarSinBarrerasAsync(empresaId, barriosNuevos);
        await ValidarCorredorSinBarrerasAsync(empresaId, zona.CorredorVialId, barriosNuevos, zonaIdActual: zonaId);

        zona.Barrios = barriosNuevos;
        await _zonaRepositorio.GuardarCambiosAsync();

        var zonaActualizada = await _zonaRepositorio.ObtenerPorIdAsync(zona.ZonaId);
        return ZonaMapper.AZonaDto(zonaActualizada!);
    }

    /// <inheritdoc />
    public async Task ActivarAsync(int empresaId, int zonaId)
    {
        var zona = await ObtenerZonaDeLaEmpresaOFallarAsync(empresaId, zonaId);
        zona.Activa = true;
        await _zonaRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task DesactivarAsync(int empresaId, int zonaId)
    {
        var zona = await ObtenerZonaDeLaEmpresaOFallarAsync(empresaId, zonaId);
        zona.Activa = false;
        await _zonaRepositorio.GuardarCambiosAsync();
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

    /// <summary>Recorta espacios y descarta barrios vacíos o repetidos, sin decidir a qué zona pertenece cada uno.</summary>
    private static List<string> LimpiarBarrios(List<string> barrios) =>
        barrios
            .Select(b => b.Trim())
            .Where(b => b.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Impide guardar una zona que combine dos barrios entre los que la empresa
    /// declaró una <see cref="BarreraGeografica"/> (por ejemplo, separados por
    /// una barrera topográfica sin vía de conexión útil).
    /// </summary>
    private async Task ValidarSinBarrerasAsync(int empresaId, List<string> barrios)
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
    /// Impide asignar esta zona a un <see cref="CorredorVial"/> si alguno de
    /// sus barrios tiene una barrera geográfica declarada con algún barrio de
    /// otra zona que ya esté en ese mismo corredor (un corredor agrupa zonas
    /// para que compartan vehículo, así que nunca puede mezclar barrios que
    /// no tienen conexión real entre sí).
    /// </summary>
    private async Task ValidarCorredorSinBarrerasAsync(int empresaId, int? corredorVialId, List<string> barrios, int? zonaIdActual)
    {
        if (corredorVialId is null)
        {
            return;
        }

        var zonasDelCorredor = (await _zonaRepositorio.ObtenerPorEmpresaAsync(empresaId))
            .Where(z => z.CorredorVialId == corredorVialId && z.ZonaId != zonaIdActual)
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
