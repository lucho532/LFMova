namespace LFMova.Application.DTOs.Servicios;

/// <summary>
/// Ubicación real del dispositivo del conductor al finalizar el servicio.
/// Es obligatoria: sin ambas coordenadas el servicio no se finaliza (los tipos
/// admiten nulo solo para poder rechazar un cuerpo incompleto con un mensaje claro).
/// </summary>
public class FinalizarServicioDto
{
    /// <summary>Latitud del conductor al finalizar. Obligatoria.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud del conductor al finalizar. Obligatoria.</summary>
    public double? Longitud { get; set; }
}
