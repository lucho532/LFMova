using TransportApp.Application.DTOs.Zonas;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="Zona"/> y su DTO. No contiene reglas de
/// negocio ni realiza consultas de persistencia.
/// </summary>
public static class ZonaMapper
{
    /// <summary>Convierte una entidad <see cref="Zona"/> en su DTO público.</summary>
    public static ZonaDto AZonaDto(Zona zona) => new()
    {
        ZonaId = zona.ZonaId,
        EmpresaId = zona.EmpresaId,
        Nombre = zona.Nombre,
        Barrios = new List<string>(zona.Barrios),
        Activa = zona.Activa,
        Orden = zona.Orden,
        MacroZonaId = zona.MacroZonaId,
        MacroZonaNombre = zona.MacroZona?.Nombre,
        CorredorVialId = zona.CorredorVialId,
        CorredorVialNombre = zona.CorredorVial?.Nombre
    };
}
