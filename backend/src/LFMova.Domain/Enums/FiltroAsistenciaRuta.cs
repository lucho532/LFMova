namespace LFMova.Domain.Enums;

/// <summary>
/// Qué pasajeros de una ruta incluye un informe de asistencia. No es un estado de ningún registro: solo
/// elige un subconjunto de los pasajeros según su <see cref="EstadoServicioPasajero"/> (ver
/// <c>ReglasAsistenciaRuta</c>).
/// </summary>
public enum FiltroAsistenciaRuta
{
    /// <summary>Todos los pasajeros de la ruta.</summary>
    TODOS,

    /// <summary>Solo quienes asistieron: fueron recogidos.</summary>
    ASISTIERON,

    /// <summary>Solo quienes no asistieron: no se pudieron recoger o avisaron que no asistirían.</summary>
    NO_ASISTIERON
}
