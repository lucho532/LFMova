using LFMova.Application.DTOs.Zonas;
using LFMova.Application.Interfaces;
using LFMova.Application.Mappers;
using LFMova.Domain.Entities;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de administración de corredores viales de una
/// empresa. Aplica <see cref="ReglasMultiempresa"/> para garantizar que un
/// corredor solamente se consulte o modifique dentro del contexto de su
/// propia empresa. No accede directamente a Entity Framework Core.
/// </summary>
public class CorredorVialServicio : ICorredorVialServicio
{
    private readonly ICorredorVialRepositorio _corredorVialRepositorio;

    /// <summary>Crea el servicio con su repositorio.</summary>
    public CorredorVialServicio(ICorredorVialRepositorio corredorVialRepositorio)
    {
        _corredorVialRepositorio = corredorVialRepositorio;
    }

    /// <inheritdoc />
    public async Task<CorredorVialDto> CrearAsync(int empresaId, CrearCorredorVialDto datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Nombre))
        {
            throw new InvalidOperationException("El nombre del corredor vial es obligatorio.");
        }

        var corredor = new CorredorVial
        {
            EmpresaId = empresaId,
            Nombre = datos.Nombre.Trim(),
            Activo = true
        };

        await _corredorVialRepositorio.AgregarAsync(corredor);
        await _corredorVialRepositorio.GuardarCambiosAsync();

        return CorredorVialMapper.ACorredorVialDto(corredor);
    }

    /// <inheritdoc />
    public async Task<List<CorredorVialDto>> ObtenerPorEmpresaAsync(int empresaId)
    {
        var corredores = await _corredorVialRepositorio.ObtenerPorEmpresaAsync(empresaId);
        return corredores.Select(CorredorVialMapper.ACorredorVialDto).ToList();
    }

    /// <inheritdoc />
    public async Task ActivarAsync(int empresaId, int corredorVialId)
    {
        var corredor = await ObtenerCorredorDeLaEmpresaOFallarAsync(empresaId, corredorVialId);
        corredor.Activo = true;
        await _corredorVialRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task DesactivarAsync(int empresaId, int corredorVialId)
    {
        var corredor = await ObtenerCorredorDeLaEmpresaOFallarAsync(empresaId, corredorVialId);
        corredor.Activo = false;
        await _corredorVialRepositorio.GuardarCambiosAsync();
    }

    private async Task<CorredorVial> ObtenerCorredorDeLaEmpresaOFallarAsync(int empresaId, int corredorVialId)
    {
        var corredor = await _corredorVialRepositorio.ObtenerPorIdAsync(corredorVialId);
        if (corredor is null || !ReglasMultiempresa.CorredorVialPerteneceAEmpresa(corredor, empresaId))
        {
            throw new InvalidOperationException("El corredor vial indicado no existe en esta empresa.");
        }

        return corredor;
    }
}
