namespace LFMova.Application.DTOs.ServiciosPasajero;

/// <summary>Servicio (ruta) al que el coordinador arrastra y suelta a un pasajero.</summary>
public class MoverServicioPasajeroDto
{
    /// <summary>Identificador del servicio destino.</summary>
    public int ServicioDestinoId { get; set; }
}
