namespace TransportApp.Application.DTOs.ServiciosPasajero;

/// <summary>Unidad operativa a la que el coordinador mueve a un pasajero.</summary>
public class ReasignarServicioPasajeroDto
{
    /// <summary>Unidad operativa destino.</summary>
    public int UnidadOperativaId { get; set; }
}
