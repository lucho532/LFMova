using TransportApp.Application.DTOs.Zonas;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="BarreraGeografica"/> y su DTO. No contiene
/// reglas de negocio ni realiza consultas de persistencia.
/// </summary>
public static class BarreraGeograficaMapper
{
    /// <summary>Convierte una entidad <see cref="BarreraGeografica"/> en su DTO público.</summary>
    public static BarreraGeograficaDto ABarreraGeograficaDto(BarreraGeografica barrera) => new()
    {
        BarreraGeograficaId = barrera.BarreraGeograficaId,
        EmpresaId = barrera.EmpresaId,
        BarrioA = barrera.BarrioA,
        BarrioB = barrera.BarrioB,
        Motivo = barrera.Motivo
    };
}
