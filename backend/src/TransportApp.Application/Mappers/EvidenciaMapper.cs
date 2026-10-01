using TransportApp.Application.DTOs.Incidencias;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="Evidencia"/> y su DTO. No contiene reglas de
/// negocio ni realiza consultas de persistencia.
/// </summary>
public static class EvidenciaMapper
{
    /// <summary>Convierte una entidad <see cref="Evidencia"/> en su DTO público.</summary>
    public static EvidenciaDto AEvidenciaDto(Evidencia evidencia) => new()
    {
        EvidenciaId = evidencia.EvidenciaId,
        IncidenciaId = evidencia.IncidenciaId,
        Tipo = evidencia.Tipo,
        ReferenciaArchivo = evidencia.ReferenciaArchivo,
        FechaHora = evidencia.FechaHora
    };
}
