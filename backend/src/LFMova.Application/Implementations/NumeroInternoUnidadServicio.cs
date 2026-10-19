using LFMova.Application.DTOs.UnidadesOperativas;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa la asignación del número interno que una empresa le da a las unidades de sus conductores.
/// Verifica que la unidad pertenezca a la empresa (conductor con vinculación activa) para no tocar los
/// datos de otra, y que el número no se repita dentro de la empresa. No decide autorización de rol.
/// </summary>
public class NumeroInternoUnidadServicio : INumeroInternoUnidadServicio
{
    /// <summary>Longitud máxima del número interno.</summary>
    public const int LongitudMaxima = 30;

    private readonly INumeroInternoUnidadRepositorio _repositorio;
    private readonly IUnidadOperativaRepositorio _unidadRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;

    /// <summary>Crea el servicio con sus repositorios.</summary>
    public NumeroInternoUnidadServicio(
        INumeroInternoUnidadRepositorio repositorio, IUnidadOperativaRepositorio unidadRepositorio, IConductorRepositorio conductorRepositorio)
    {
        _repositorio = repositorio;
        _unidadRepositorio = unidadRepositorio;
        _conductorRepositorio = conductorRepositorio;
    }

    /// <inheritdoc />
    public async Task<List<NumeroInternoUnidadDto>> ObtenerPorEmpresaAsync(int empresaId)
        => (await _repositorio.ObtenerPorEmpresaAsync(empresaId))
            .Select(n => new NumeroInternoUnidadDto { UnidadOperativaId = n.UnidadOperativaId, Numero = n.Numero })
            .ToList();

    /// <inheritdoc />
    public async Task AsignarAsync(int empresaId, int unidadOperativaId, string? numero)
    {
        var unidad = await _unidadRepositorio.ObtenerPorIdAsync(unidadOperativaId);
        var conductor = unidad is null ? null : await _conductorRepositorio.ObtenerPorIdAsync(unidad.ConductorId);
        if (unidad is null || conductor is null || !conductor.VinculacionesConductorEmpresa.Any(v => v.EmpresaId == empresaId && v.Activa))
        {
            throw new InvalidOperationException("La unidad operativa indicada no existe en esta empresa.");
        }

        var limpio = numero?.Trim() ?? string.Empty;
        var actual = await _repositorio.ObtenerAsync(empresaId, unidadOperativaId);

        if (limpio.Length == 0)
        {
            if (actual is not null)
            {
                _repositorio.Eliminar(actual);
                await _repositorio.GuardarCambiosAsync();
            }

            return;
        }

        if (limpio.Length > LongitudMaxima)
        {
            throw new InvalidOperationException($"El número interno no puede tener más de {LongitudMaxima} caracteres.");
        }

        if (await _repositorio.ExisteEnOtraUnidadAsync(empresaId, limpio, unidadOperativaId))
        {
            throw new InvalidOperationException("Ese número interno ya lo tiene otro vehículo de la empresa.");
        }

        if (actual is null)
        {
            await _repositorio.AgregarAsync(new NumeroInternoUnidad { EmpresaId = empresaId, UnidadOperativaId = unidadOperativaId, Numero = limpio });
        }
        else
        {
            actual.Numero = limpio;
        }

        await _repositorio.GuardarCambiosAsync();
    }
}
