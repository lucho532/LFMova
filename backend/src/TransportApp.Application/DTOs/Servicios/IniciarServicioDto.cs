namespace TransportApp.Application.DTOs.Servicios;

/// <summary>
/// Ubicación real del dispositivo del conductor al iniciar el servicio.
/// Ambos campos son opcionales: si el dispositivo no entrega ubicación, el
/// servicio se inicia igual sin ese dato.
/// </summary>
public class IniciarServicioDto
{
    /// <summary>Latitud del conductor al iniciar, si se pudo obtener.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud del conductor al iniciar, si se pudo obtener.</summary>
    public double? Longitud { get; set; }
}
