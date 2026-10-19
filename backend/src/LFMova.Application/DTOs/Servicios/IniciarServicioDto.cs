namespace LFMova.Application.DTOs.Servicios;

/// <summary>
/// Ubicación real del dispositivo del conductor al iniciar el servicio.
/// Es obligatoria: sin ambas coordenadas el servicio no se inicia (los tipos
/// admiten nulo solo para poder rechazar un cuerpo incompleto con un mensaje claro).
/// </summary>
public class IniciarServicioDto
{
    /// <summary>Latitud del conductor al iniciar. Obligatoria.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud del conductor al iniciar. Obligatoria.</summary>
    public double? Longitud { get; set; }
}
