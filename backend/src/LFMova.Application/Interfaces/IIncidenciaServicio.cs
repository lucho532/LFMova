using LFMova.Application.DTOs.Incidencias;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define el registro de incidencias y evidencias sobre un
/// <c>ServicioPasajero</c> durante la ejecución de un servicio (ver
/// <c>data-model.md</c> §19/§20 y <c>tasks.md</c> T091/T092/T093). No decide
/// autorización de acceso al endpoint: eso se verifica en la capa de Api
/// usando <see cref="ObtenerUsuarioIdConductorAsync"/>.
/// </summary>
public interface IIncidenciaServicio
{
    /// <summary>
    /// Obtiene el <c>UsuarioId</c> del conductor de la unidad operativa
    /// asignada al servicio del pasajero indicado, o <c>null</c> si no puede
    /// resolverse. Se usa desde la capa de Api para autorizar el acceso.
    /// </summary>
    Task<int?> ObtenerUsuarioIdConductorAsync(int empresaId, int servicioPasajeroId);

    /// <summary>Registra una nueva incidencia sobre el pasajero indicado, incluyendo su ubicación cuando esté disponible.</summary>
    Task<IncidenciaDto> CrearAsync(int empresaId, int servicioPasajeroId, CrearIncidenciaDto datos);

    /// <summary>Obtiene las incidencias del pasajero indicado, con sus evidencias.</summary>
    Task<List<IncidenciaDto>> ObtenerPorServicioPasajeroAsync(int empresaId, int servicioPasajeroId);

    /// <summary>
    /// Obtiene las incidencias de todos los pasajeros del servicio indicado, con sus evidencias. Para
    /// que el coordinador pueda ver, dentro del detalle de una ruta, qué incidencias se reportaron.
    /// </summary>
    Task<List<IncidenciaDto>> ObtenerPorServicioAsync(int empresaId, int jornadaId, int servicioId);

    /// <summary>
    /// Asocia una nueva evidencia a la incidencia indicada. No almacena el
    /// archivo binario: <c>ReferenciaArchivo</c> apunta al almacenamiento
    /// externo que se defina técnicamente (ver <c>data-model.md</c> §20.1).
    /// Falla si la incidencia no pertenece al pasajero indicado: evita que un
    /// conductor autorizado para un pasajero adjunte evidencia a la
    /// incidencia de otro (ver <c>tasks.md</c> T096).
    /// </summary>
    Task<EvidenciaDto> AgregarEvidenciaAsync(int empresaId, int servicioPasajeroId, int incidenciaId, AgregarEvidenciaDto datos);
}
