using LFMova.Application.DTOs.Soportes;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.Jornadas;

/// <summary>
/// Genera para el coordinador el Excel con toda la programación de una
/// jornada: las rutas de todos los conductores (y las que aún no tienen
/// conductor), cada una con sus pasajeros. Solo lee y da el archivo: no
/// publica, no envía correos y no decide autorización.
/// </summary>
public class ExportadorSoporteJornada
{
    private readonly IJornadaRepositorio _jornadaRepositorio;
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IEmpresaRepositorio _empresaRepositorio;
    private readonly ArmadorSoporteRutas _armador;
    private readonly IGeneradorSoporteRutas _generador;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public ExportadorSoporteJornada(
        IJornadaRepositorio jornadaRepositorio,
        IServicioRepositorio servicioRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        IEmpresaRepositorio empresaRepositorio,
        ArmadorSoporteRutas armador,
        IGeneradorSoporteRutas generador)
    {
        _jornadaRepositorio = jornadaRepositorio;
        _servicioRepositorio = servicioRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _empresaRepositorio = empresaRepositorio;
        _armador = armador;
        _generador = generador;
    }

    /// <summary>
    /// Devuelve el Excel con todas las rutas no canceladas de la jornada, en el orden en que ocurren.
    /// Falla si la jornada no es de la empresa.
    /// </summary>
    public async Task<ArchivoSoporte> ExportarAsync(int empresaId, int jornadaId)
    {
        var jornada = await _jornadaRepositorio.ObtenerPorIdAsync(jornadaId);
        if (jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(jornada, empresaId))
        {
            throw new InvalidOperationException("La jornada indicada no existe en esta empresa.");
        }

        var empresa = await _empresaRepositorio.ObtenerPorIdAsync(empresaId);
        var soporte = new SoporteRutas
        {
            NombreEmpresa = empresa?.Nombre ?? string.Empty,
            FechaOperativa = jornada.FechaOperativa
        };

        var rutas = (await _servicioRepositorio.ObtenerPorJornadaAsync(jornadaId))
            .Where(s => s.Estado != EstadoServicio.CANCELADO)
            .OrderBy(s => s.Fecha).ThenBy(s => s.HoraProgramada).ThenBy(s => s.ServicioId)
            .ToList();

        var nombrePorUnidad = new Dictionary<int, string>();
        foreach (var ruta in rutas)
        {
            soporte.Rutas.Add(await _armador.ArmarAsync(ruta, await ObtenerNombreConductorAsync(ruta.UnidadOperativaId, nombrePorUnidad)));
        }

        return new ArchivoSoporte($"programacion-{jornada.FechaOperativa:yyyy-MM-dd}.xlsx", _generador.Generar(soporte));
    }

    private async Task<string> ObtenerNombreConductorAsync(int? unidadOperativaId, Dictionary<int, string> nombrePorUnidad)
    {
        const string sinConductor = "Sin conductor";
        if (unidadOperativaId is null)
        {
            return sinConductor;
        }

        if (!nombrePorUnidad.TryGetValue(unidadOperativaId.Value, out var nombre))
        {
            var unidad = await _unidadOperativaRepositorio.ObtenerPorIdAsync(unidadOperativaId.Value);
            var conductor = unidad is null ? null : await _conductorRepositorio.ObtenerPorIdAsync(unidad.ConductorId);
            nombre = string.IsNullOrWhiteSpace(conductor?.NombreCompleto) ? sinConductor : conductor.NombreCompleto;
            nombrePorUnidad[unidadOperativaId.Value] = nombre;
        }

        return nombre;
    }
}
