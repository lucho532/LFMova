namespace LFMova.Application.DTOs.ServiciosPasajero;

/// <summary>Datos necesarios para que el conductor reordene un pasajero dentro del servicio.</summary>
public class ReordenarServicioPasajeroDto
{
    /// <summary>Nuevo valor de orden operativo para el pasajero.</summary>
    public int NuevoOrden { get; set; }
}
