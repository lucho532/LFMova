using LFMova.Application.DTOs.Incidencias;
using LFMova.Domain.Entities;

namespace LFMova.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="Incidencia"/> y su DTO. No contiene reglas de
/// negocio ni realiza consultas de persistencia.
/// </summary>
public static class IncidenciaMapper
{
    /// <summary>
    /// Convierte una entidad <see cref="Incidencia"/> en su DTO público. Las
    /// <paramref name="evidencias"/> son opcionales: cuando no se
    /// proporcionan, el DTO se devuelve sin evidencias asociadas.
    /// </summary>
    public static IncidenciaDto AIncidenciaDto(Incidencia incidencia, List<Evidencia>? evidencias = null) => new()
    {
        IncidenciaId = incidencia.IncidenciaId,
        ServicioPasajeroId = incidencia.ServicioPasajeroId,
        Tipo = incidencia.Tipo,
        Descripcion = incidencia.Descripcion,
        FechaHora = incidencia.FechaHora,
        Latitud = incidencia.Latitud,
        Longitud = incidencia.Longitud,
        Evidencias = (evidencias ?? []).Select(EvidenciaMapper.AEvidenciaDto).ToList()
    };
}
