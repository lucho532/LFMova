namespace LFMova.Domain.Entities;

/// <summary>
/// Representa una ubicación anteriormente utilizada por un empleado como
/// dirección de recogida. Se diferencia de <c>Empleado.Direccion</c> (la
/// dirección actual/habitual) y de <c>ServicioPasajero.DireccionRecogida</c>
/// (la dirección exacta e inmutable utilizada en un servicio concreto).
/// No decide cuál es la ubicación habitual actual del empleado.
/// </summary>
public class UbicacionRecogidaHistorica
{
    /// <summary>Identificador único de la ubicación histórica.</summary>
    public int UbicacionRecogidaHistoricaId { get; set; }

    /// <summary>Empleado al que pertenece esta ubicación histórica.</summary>
    public int EmpleadoId { get; set; }

    /// <summary>Dirección registrada.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Barrio registrado.</summary>
    public string Barrio { get; set; } = string.Empty;

    /// <summary>Latitud registrada. Puede ser <c>null</c> si no está disponible.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud registrada. Puede ser <c>null</c> si no está disponible.</summary>
    public double? Longitud { get; set; }

    /// <summary>
    /// Momento en que se registró/utilizó esta ubicación. Permite determinar
    /// cuál es la ubicación histórica más reciente.
    /// </summary>
    public DateTime FechaRegistro { get; set; }

    /// <summary>Empleado al que pertenece esta ubicación histórica.</summary>
    public Empleado? Empleado { get; set; }
}
