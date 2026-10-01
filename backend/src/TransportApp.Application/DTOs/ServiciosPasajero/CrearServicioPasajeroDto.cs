namespace TransportApp.Application.DTOs.ServiciosPasajero;

/// <summary>
/// Datos necesarios para asignar una programación de transporte a un
/// servicio, creando su <c>ServicioPasajero</c>.
/// </summary>
public class CrearServicioPasajeroDto
{
    /// <summary>Programación de transporte que se asigna al servicio.</summary>
    public int ProgramacionTransporteId { get; set; }

    /// <summary>Latitud de recogida para este servicio, si está disponible.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud de recogida para este servicio, si está disponible.</summary>
    public double? Longitud { get; set; }
}
