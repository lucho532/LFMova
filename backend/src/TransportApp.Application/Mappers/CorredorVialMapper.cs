using TransportApp.Application.DTOs.Zonas;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="CorredorVial"/> y su DTO. No contiene reglas
/// de negocio ni realiza consultas de persistencia.
/// </summary>
public static class CorredorVialMapper
{
    /// <summary>Convierte una entidad <see cref="CorredorVial"/> en su DTO público.</summary>
    public static CorredorVialDto ACorredorVialDto(CorredorVial corredor) => new()
    {
        CorredorVialId = corredor.CorredorVialId,
        EmpresaId = corredor.EmpresaId,
        Nombre = corredor.Nombre,
        Activo = corredor.Activo
    };
}
