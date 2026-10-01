using LFMova.Application.DTOs.Zonas;
using LFMova.Application.Interfaces;
using LFMova.Application.Mappers;
using LFMova.Domain.Entities;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de administración de barreras geográficas
/// entre barrios de una empresa. Aplica <see cref="ReglasMultiempresa"/> para
/// garantizar que una barrera solamente se consulte o elimine dentro del
/// contexto de su propia empresa. No accede directamente a Entity Framework
/// Core.
/// </summary>
public class BarreraGeograficaServicio : IBarreraGeograficaServicio
{
    private readonly IBarreraGeograficaRepositorio _barreraGeograficaRepositorio;

    /// <summary>Crea el servicio con su repositorio.</summary>
    public BarreraGeograficaServicio(IBarreraGeograficaRepositorio barreraGeograficaRepositorio)
    {
        _barreraGeograficaRepositorio = barreraGeograficaRepositorio;
    }

    /// <inheritdoc />
    public async Task<BarreraGeograficaDto> CrearAsync(int empresaId, CrearBarreraGeograficaDto datos)
    {
        var barrioA = datos.BarrioA.Trim();
        var barrioB = datos.BarrioB.Trim();

        if (barrioA.Length == 0 || barrioB.Length == 0)
        {
            throw new InvalidOperationException("Los dos barrios de la barrera son obligatorios.");
        }

        if (string.Equals(barrioA, barrioB, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Los dos barrios de una barrera deben ser distintos.");
        }

        var existentes = await _barreraGeograficaRepositorio.ObtenerPorEmpresaAsync(empresaId);
        if (ReglasBarrerasGeograficas.HayBarreraEntre(existentes, barrioA, barrioB))
        {
            throw new InvalidOperationException("Ya existe una barrera geográfica declarada entre estos dos barrios.");
        }

        var barrera = new BarreraGeografica
        {
            EmpresaId = empresaId,
            BarrioA = barrioA,
            BarrioB = barrioB,
            Motivo = string.IsNullOrWhiteSpace(datos.Motivo) ? null : datos.Motivo.Trim()
        };

        await _barreraGeograficaRepositorio.AgregarAsync(barrera);
        await _barreraGeograficaRepositorio.GuardarCambiosAsync();

        return BarreraGeograficaMapper.ABarreraGeograficaDto(barrera);
    }

    /// <inheritdoc />
    public async Task<List<BarreraGeograficaDto>> ObtenerPorEmpresaAsync(int empresaId)
    {
        var barreras = await _barreraGeograficaRepositorio.ObtenerPorEmpresaAsync(empresaId);
        return barreras.Select(BarreraGeograficaMapper.ABarreraGeograficaDto).ToList();
    }

    /// <inheritdoc />
    public async Task EliminarAsync(int empresaId, int barreraGeograficaId)
    {
        var barrera = await _barreraGeograficaRepositorio.ObtenerPorIdAsync(barreraGeograficaId);
        if (barrera is null || !ReglasMultiempresa.BarreraGeograficaPerteneceAEmpresa(barrera, empresaId))
        {
            throw new InvalidOperationException("La barrera geográfica indicada no existe en esta empresa.");
        }

        _barreraGeograficaRepositorio.Eliminar(barrera);
        await _barreraGeograficaRepositorio.GuardarCambiosAsync();
    }
}
