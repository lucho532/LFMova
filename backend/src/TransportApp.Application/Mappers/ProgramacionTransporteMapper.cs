using TransportApp.Application.DTOs.Programaciones;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="ProgramacionTransporte"/> y su DTO. No contiene
/// reglas de negocio ni realiza consultas de persistencia.
/// </summary>
public static class ProgramacionTransporteMapper
{
    /// <summary>Convierte una entidad <see cref="ProgramacionTransporte"/> en su DTO público.</summary>
    public static ProgramacionDto AProgramacionDto(ProgramacionTransporte programacion) => new()
    {
        ProgramacionTransporteId = programacion.ProgramacionTransporteId,
        EmpresaId = programacion.EmpresaId,
        EmpleadoId = programacion.EmpleadoId,
        SedeId = programacion.SedeId,
        Fecha = programacion.Fecha,
        Hora = programacion.Hora,
        Tipo = programacion.Tipo,
        DireccionRecogida = programacion.DireccionRecogida,
        BarrioRecogida = programacion.BarrioRecogida
    };
}
