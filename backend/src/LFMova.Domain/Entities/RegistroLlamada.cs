namespace LFMova.Domain.Entities;

/// <summary>
/// Deja constancia de que el conductor pulsó "Llamar" sobre un pasajero de
/// un servicio: cuándo, desde dónde y cuánto estuvo aproximadamente fuera de
/// la aplicación. No es el historial de llamadas del teléfono: la llamada
/// ocurre en el marcador del dispositivo, así que no sabe si el pasajero
/// contestó ni mide la duración exacta.
/// </summary>
public class RegistroLlamada
{
    /// <summary>Identificador único del registro.</summary>
    public int RegistroLlamadaId { get; set; }

    /// <summary>Pasajero del servicio al que se llamó.</summary>
    public int ServicioPasajeroId { get; set; }

    /// <summary>Instante (UTC) en que el conductor pulsó "Llamar".</summary>
    public DateTime FechaHora { get; set; }

    /// <summary>
    /// Segundos que el conductor estuvo en el marcador antes de volver a la
    /// aplicación (incluye el tiempo que timbró). Es <c>null</c> mientras no
    /// vuelva o si el dispositivo no pudo medirlo.
    /// </summary>
    public int? DuracionAproximadaSegundos { get; set; }

    /// <summary>Latitud del conductor al llamar. Puede ser <c>null</c> si no está disponible.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud del conductor al llamar. Puede ser <c>null</c> si no está disponible.</summary>
    public double? Longitud { get; set; }

    /// <summary>Pasajero del servicio al que se llamó.</summary>
    public ServicioPasajero? ServicioPasajero { get; set; }
}
