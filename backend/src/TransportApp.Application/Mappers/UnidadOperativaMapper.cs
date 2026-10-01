using TransportApp.Application.DTOs.UnidadesOperativas;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="UnidadOperativa"/> y su DTO. No contiene reglas
/// de negocio ni realiza consultas de persistencia.
/// </summary>
public static class UnidadOperativaMapper
{
    /// <summary>Convierte una entidad <see cref="UnidadOperativa"/> en su DTO público.</summary>
    public static UnidadOperativaDto AUnidadOperativaDto(UnidadOperativa unidadOperativa) => new()
    {
        UnidadOperativaId = unidadOperativa.UnidadOperativaId,
        ConductorId = unidadOperativa.ConductorId,
        VehiculoId = unidadOperativa.VehiculoId,
        Activa = unidadOperativa.Activa
    };
}
