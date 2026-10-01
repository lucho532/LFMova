using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre <see cref="Servicio"/>
/// para la capa de aplicación. No decide reglas de negocio ni de autorización.
/// </summary>
public interface IServicioRepositorio
{
    /// <summary>Obtiene un servicio por su identificador, o <c>null</c> si no existe.</summary>
    Task<Servicio?> ObtenerPorIdAsync(int servicioId);

    /// <summary>Obtiene los servicios de la jornada indicada.</summary>
    Task<List<Servicio>> ObtenerPorJornadaAsync(int jornadaId);

    /// <summary>
    /// Obtiene los servicios actualmente asignados a la unidad operativa
    /// indicada (en cualquier jornada), para validar conflictos temporales.
    /// </summary>
    Task<List<Servicio>> ObtenerPorUnidadOperativaAsync(int unidadOperativaId);

    /// <summary>
    /// Obtiene los servicios en estado <c>PUBLICADO</c> del tipo indicado (en
    /// cualquier jornada). Se usa para la alerta de ruta no iniciada (ver
    /// <c>tasks.md</c> T094).
    /// </summary>
    Task<List<Servicio>> ObtenerPublicadosPorTipoAsync(TipoServicio tipo);

    /// <summary>
    /// Obtiene los servicios en <c>PUBLICADO</c> o <c>EN_CURSO</c> de una
    /// fecha y hora programadas (de cualquier empresa), con su jornada y su
    /// sede. Se usa para avisar a los conductores que comparten horario con
    /// una ruta no iniciada.
    /// </summary>
    Task<List<Servicio>> ObtenerActivosPorFechaYHoraAsync(DateOnly fecha, TimeOnly hora);

    /// <summary>
    /// Obtiene los servicios de la empresa que no están finalizados ni
    /// cancelados y que, o todavía no se enviaron (<c>BORRADOR</c>,
    /// <c>PENDIENTE_ASIGNACION</c>, <c>ASIGNADO</c>, de cualquier fecha), o
    /// tienen fecha desde <paramref name="desde"/>. Incluye pasajeros y sede.
    /// </summary>
    Task<List<Servicio>> ObtenerPendientesDeProgramacionAsync(int empresaId, DateOnly desde);

    /// <summary>Agrega un nuevo servicio.</summary>
    Task AgregarAsync(Servicio servicio);

    /// <summary>Elimina por completo el servicio indicado (borrado físico, no un cambio de estado).</summary>
    Task EliminarAsync(Servicio servicio);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
