namespace LFMova.Application.DTOs.Servicios;

/// <summary>
/// Ubicación real del dispositivo del conductor al finalizar el servicio.
/// Ambos campos son opcionales: si el dispositivo no entrega ubicación, el
/// servicio se finaliza igual sin ese dato.
/// </summary>
public class FinalizarServicioDto
{
    /// <summary>Latitud del conductor al finalizar, si se pudo obtener.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud del conductor al finalizar, si se pudo obtener.</summary>
    public double? Longitud { get; set; }
}
