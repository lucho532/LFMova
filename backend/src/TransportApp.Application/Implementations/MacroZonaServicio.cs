using TransportApp.Application.DTOs.Zonas;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Mappers;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Rules;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de administración de macrozonas de una
/// empresa. Aplica <see cref="ReglasMultiempresa"/> para garantizar que una
/// macrozona solamente se consulte o modifique dentro del contexto de su
/// propia empresa. No accede directamente a Entity Framework Core.
/// </summary>
public class MacroZonaServicio : IMacroZonaServicio
{
    private readonly IMacroZonaRepositorio _macroZonaRepositorio;

    /// <summary>Crea el servicio con su repositorio.</summary>
    public MacroZonaServicio(IMacroZonaRepositorio macroZonaRepositorio)
    {
        _macroZonaRepositorio = macroZonaRepositorio;
    }

    /// <inheritdoc />
    public async Task<MacroZonaDto> CrearAsync(int empresaId, CrearMacroZonaDto datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Nombre))
        {
            throw new InvalidOperationException("El nombre de la macrozona es obligatorio.");
        }

        var macroZona = new MacroZona
        {
            EmpresaId = empresaId,
            Nombre = datos.Nombre.Trim(),
            Activa = true
        };

        await _macroZonaRepositorio.AgregarAsync(macroZona);
        await _macroZonaRepositorio.GuardarCambiosAsync();

        return MacroZonaMapper.AMacroZonaDto(macroZona);
    }

    /// <inheritdoc />
    public async Task<List<MacroZonaDto>> ObtenerPorEmpresaAsync(int empresaId)
    {
        var macroZonas = await _macroZonaRepositorio.ObtenerPorEmpresaAsync(empresaId);
        return macroZonas.Select(MacroZonaMapper.AMacroZonaDto).ToList();
    }

    /// <inheritdoc />
    public async Task ActivarAsync(int empresaId, int macroZonaId)
    {
        var macroZona = await ObtenerMacroZonaDeLaEmpresaOFallarAsync(empresaId, macroZonaId);
        macroZona.Activa = true;
        await _macroZonaRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task DesactivarAsync(int empresaId, int macroZonaId)
    {
        var macroZona = await ObtenerMacroZonaDeLaEmpresaOFallarAsync(empresaId, macroZonaId);
        macroZona.Activa = false;
        await _macroZonaRepositorio.GuardarCambiosAsync();
    }

    private async Task<MacroZona> ObtenerMacroZonaDeLaEmpresaOFallarAsync(int empresaId, int macroZonaId)
    {
        var macroZona = await _macroZonaRepositorio.ObtenerPorIdAsync(macroZonaId);
        if (macroZona is null || !ReglasMultiempresa.MacroZonaPerteneceAEmpresa(macroZona, empresaId))
        {
            throw new InvalidOperationException("La macrozona indicada no existe en esta empresa.");
        }

        return macroZona;
    }
}
