using LFMova.Application.DTOs.Zonas;
using LFMova.Domain.Entities;

namespace LFMova.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="MacroZona"/> y su DTO. No contiene reglas de
/// negocio ni realiza consultas de persistencia.
/// </summary>
public static class MacroZonaMapper
{
    /// <summary>Convierte una entidad <see cref="MacroZona"/> en su DTO público.</summary>
    public static MacroZonaDto AMacroZonaDto(MacroZona macroZona) => new()
    {
        MacroZonaId = macroZona.MacroZonaId,
        EmpresaId = macroZona.EmpresaId,
        Nombre = macroZona.Nombre,
        Activa = macroZona.Activa
    };
}
