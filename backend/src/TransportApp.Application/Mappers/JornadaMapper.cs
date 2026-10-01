using TransportApp.Application.DTOs.Jornadas;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="Jornada"/> y su DTO. No contiene reglas de
/// negocio ni realiza consultas de persistencia.
/// </summary>
public static class JornadaMapper
{
    /// <summary>Convierte una entidad <see cref="Jornada"/> en su DTO público.</summary>
    public static JornadaDto AJornadaDto(Jornada jornada) => new()
    {
        JornadaId = jornada.JornadaId,
        EmpresaId = jornada.EmpresaId,
        FechaOperativa = jornada.FechaOperativa
    };
}
