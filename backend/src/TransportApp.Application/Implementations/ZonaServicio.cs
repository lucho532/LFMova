using TransportApp.Application.DTOs.Zonas;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Mappers;
using TransportApp.Application.Validators;
using TransportApp.Domain.Entities;
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
    private readonly ValidadorBarrerasZona _validadorBarreras;

    /// <summary>Crea el servicio con su repositorio.</summary>
    public ZonaServicio(IZonaRepositorio zonaRepositorio, IBarreraGeograficaRepositorio barreraGeograficaRepositorio)
    {
        _zonaRepositorio = zonaRepositorio;
        _validadorBarreras = new ValidadorBarrerasZona(zonaRepositorio, barreraGeograficaRepositorio);
    }

    /// <inheritdoc />
    public async Task<ZonaDto> CrearAsync(int empresaId, CrearZonaDto datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Nombre))
        {
            throw new InvalidOperationException("El nombre de la zona es obligatorio.");
        }

        var barrios = LimpiarBarrios(datos.Barrios);
        await _validadorBarreras.ValidarSinBarrerasAsync(empresaId, barrios);
        await _validadorBarreras.ValidarCorredorSinBarrerasAsync(empresaId, datos.CorredorVialId, barrios);

        var zona = new Zona
        {
            EmpresaId = empresaId,
            Nombre = datos.Nombre.Trim(),
            Barrios = barrios,
            MacroZonaId = datos.MacroZonaId,
            CorredorVialId = datos.CorredorVialId,
            Activa = true,
            // La zona nueva queda al final de la lista.
            Orden = (await _zonaRepositorio.ObtenerPorEmpresaAsync(empresaId)).Select(z => z.Orden).DefaultIfEmpty(-1).Max() + 1
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
        return zonas.OrderBy(z => z.Orden).ThenBy(z => z.ZonaId).Select(ZonaMapper.AZonaDto).ToList();
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
        await _validadorBarreras.ValidarSinBarrerasAsync(empresaId, barrios);
        await _validadorBarreras.ValidarCorredorSinBarrerasAsync(empresaId, datos.CorredorVialId, barrios, zonaId);

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
        await _validadorBarreras.ValidarSinBarrerasAsync(empresaId, barriosNuevos);
        await _validadorBarreras.ValidarCorredorSinBarrerasAsync(empresaId, zona.CorredorVialId, barriosNuevos, zonaId);

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
}
