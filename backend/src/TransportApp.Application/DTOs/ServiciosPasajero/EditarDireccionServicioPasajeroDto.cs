namespace TransportApp.Application.DTOs.ServiciosPasajero;

/// <summary>
/// Corrige la dirección de recogida de este servicio puntual (por ejemplo,
/// un dato mal pegado al crear la ruta). No cambia el estado del pasajero ni
/// notifica a nadie: a diferencia de <see cref="ConfirmarServicioPasajeroDto"/>,
/// esto no es una confirmación de asistencia, es una corrección de datos.
/// </summary>
public class EditarDireccionServicioPasajeroDto
{
    /// <summary>Nueva dirección de recogida para este servicio.</summary>
    public string Direccion { get; set; } = string.Empty;
}
