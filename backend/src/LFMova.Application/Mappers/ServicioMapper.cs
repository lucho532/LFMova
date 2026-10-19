using LFMova.Application.DTOs.Servicios;
using LFMova.Domain.Entities;

namespace LFMova.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="Servicio"/> y su DTO. No contiene reglas de
/// negocio ni realiza consultas de persistencia.
/// </summary>
public static class ServicioMapper
{
    /// <summary>
    /// Convierte una entidad <see cref="Servicio"/> en su DTO público.
    /// <paramref name="empresaId"/> se recibe explícitamente porque
    /// <see cref="Servicio"/> no almacena su propia empresa: se obtiene a
    /// través de <see cref="Servicio.Jornada"/>.
    /// </summary>
    public static ServicioDto AServicioDto(Servicio servicio, int empresaId) => new()
    {
        ServicioId = servicio.ServicioId,
        EmpresaId = empresaId,
        JornadaId = servicio.JornadaId,
        UnidadOperativaId = servicio.UnidadOperativaId,
        SedeId = servicio.SedeId,
        NombreSede = servicio.Sede?.Nombre ?? string.Empty,
        Fecha = servicio.Fecha,
        HoraProgramada = servicio.HoraProgramada,
        Tipo = servicio.Tipo,
        Estado = servicio.Estado,
        HoraInicioReal = servicio.HoraInicioReal,
        HoraFinReal = servicio.HoraFinReal,
        LatitudInicio = servicio.LatitudInicio,
        LongitudInicio = servicio.LongitudInicio,
        LatitudFinalizacion = servicio.LatitudFinalizacion,
        LongitudFinalizacion = servicio.LongitudFinalizacion,
        CantidadPasajeros = servicio.ServiciosPasajero.Count
    };
}
