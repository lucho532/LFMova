namespace LFMova.Application.DTOs.Llamadas;

/// <summary>Representación pública de una llamada del conductor a un pasajero.</summary>
public class RegistroLlamadaDto
{
    /// <summary>Identificador único del registro.</summary>
    public int RegistroLlamadaId { get; set; }

    /// <summary>Pasajero del servicio al que se llamó.</summary>
    public int ServicioPasajeroId { get; set; }

    /// <summary>Instante (UTC) en que el conductor pulsó "Llamar".</summary>
    public DateTime FechaHora { get; set; }

    /// <summary>Segundos aproximados que duró (incluye el tiempo que timbró), si se pudieron medir.</summary>
    public int? DuracionAproximadaSegundos { get; set; }

    /// <summary>Latitud del conductor al llamar, si está disponible.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud del conductor al llamar, si está disponible.</summary>
    public double? Longitud { get; set; }
}

/// <summary>Datos que envía el conductor al pulsar "Llamar". La fecha y hora las pone el servidor.</summary>
public class RegistrarLlamadaDto
{
    /// <summary>Latitud del conductor en ese momento, si el dispositivo la entrega.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud del conductor en ese momento, si el dispositivo la entrega.</summary>
    public double? Longitud { get; set; }
}

/// <summary>Duración aproximada que el dispositivo midió al volver el conductor a la aplicación.</summary>
public class DuracionLlamadaDto
{
    /// <summary>Segundos que el conductor estuvo fuera de la aplicación.</summary>
    public int Segundos { get; set; }
}
