using TransportApp.Domain.Enums;

namespace TransportApp.Application.DTOs.ServiciosPasajero;

/// <summary>Datos necesarios para registrar el resultado del procesamiento de un pasajero.</summary>
public class CambiarEstadoServicioPasajeroDto
{
    /// <summary>Estado al que se solicita mover al pasajero.</summary>
    public EstadoServicioPasajero NuevoEstado { get; set; }
}
