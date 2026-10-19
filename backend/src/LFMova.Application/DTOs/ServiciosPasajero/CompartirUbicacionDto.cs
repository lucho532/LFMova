namespace LFMova.Application.DTOs.ServiciosPasajero;

/// <summary>Datos necesarios para que el empleado comparta su ubicación actualizada.</summary>
public class CompartirUbicacionDto
{
    /// <summary>Latitud compartida.</summary>
    public double Latitud { get; set; }

    /// <summary>Longitud compartida.</summary>
    public double Longitud { get; set; }
}
