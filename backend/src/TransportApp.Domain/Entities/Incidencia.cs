using TransportApp.Domain.Enums;

namespace TransportApp.Domain.Entities;

/// <summary>
/// Representa un problema ocurrido durante la operación de un pasajero,
/// asociado a un <c>ServicioPasajero</c> concreto. Puede tener múltiples
/// <c>Evidencia</c>. No decide su propia resolución ni notifica por sí
/// misma; esas operaciones corresponden a la capa de aplicación.
/// </summary>
public class Incidencia
{
    /// <summary>Identificador único de la incidencia.</summary>
    public int IncidenciaId { get; set; }

    /// <summary>Servicio pasajero sobre el que se registra la incidencia.</summary>
    public int ServicioPasajeroId { get; set; }

    /// <summary>Tipo de incidencia registrada.</summary>
    public TipoIncidencia Tipo { get; set; }

    /// <summary>Descripción de la incidencia.</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Fecha y hora en que ocurrió la incidencia.</summary>
    public DateTime FechaHora { get; set; }

    /// <summary>Latitud del lugar donde ocurrió la incidencia. Puede ser <c>null</c> si no está disponible.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud del lugar donde ocurrió la incidencia. Puede ser <c>null</c> si no está disponible.</summary>
    public double? Longitud { get; set; }

    /// <summary>Servicio pasajero sobre el que se registra la incidencia.</summary>
    public ServicioPasajero? ServicioPasajero { get; set; }

    /// <summary>Evidencias asociadas a esta incidencia.</summary>
    public ICollection<Evidencia> Evidencias { get; set; } = new List<Evidencia>();
}
