namespace TransportApp.Application.DTOs.ServiciosPasajero;

/// <summary>Ubicación de recogida utilizada antes por un empleado.</summary>
public class UbicacionAnteriorDto
{
    /// <summary>Dirección registrada.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Barrio registrado.</summary>
    public string Barrio { get; set; } = string.Empty;

    /// <summary>Latitud, si se guardó el punto GPS.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud, si se guardó el punto GPS.</summary>
    public double? Longitud { get; set; }

    /// <summary>Momento en que se registró.</summary>
    public DateTime FechaRegistro { get; set; }
}
