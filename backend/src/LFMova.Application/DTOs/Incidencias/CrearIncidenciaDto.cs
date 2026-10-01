using LFMova.Domain.Enums;

namespace LFMova.Application.DTOs.Incidencias;

/// <summary>Datos necesarios para registrar una incidencia sobre un pasajero.</summary>
public class CrearIncidenciaDto
{
    /// <summary>Tipo de incidencia a registrar.</summary>
    public TipoIncidencia Tipo { get; set; }

    /// <summary>Descripción de la incidencia.</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Latitud del lugar donde ocurrió la incidencia, si está disponible.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud del lugar donde ocurrió la incidencia, si está disponible.</summary>
    public double? Longitud { get; set; }
}
