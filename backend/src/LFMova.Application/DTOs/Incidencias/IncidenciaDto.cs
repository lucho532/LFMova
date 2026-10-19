using LFMova.Domain.Enums;

namespace LFMova.Application.DTOs.Incidencias;

/// <summary>Representación pública de una incidencia registrada sobre un pasajero.</summary>
public class IncidenciaDto
{
    /// <summary>Identificador único de la incidencia.</summary>
    public int IncidenciaId { get; set; }

    /// <summary>Servicio pasajero sobre el que se registró la incidencia.</summary>
    public int ServicioPasajeroId { get; set; }

    /// <summary>Tipo de incidencia registrada.</summary>
    public TipoIncidencia Tipo { get; set; }

    /// <summary>Descripción de la incidencia.</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Fecha y hora en que ocurrió la incidencia.</summary>
    public DateTime FechaHora { get; set; }

    /// <summary>Latitud del lugar donde ocurrió la incidencia, si está disponible.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud del lugar donde ocurrió la incidencia, si está disponible.</summary>
    public double? Longitud { get; set; }

    /// <summary>Evidencias asociadas a esta incidencia.</summary>
    public List<EvidenciaDto> Evidencias { get; set; } = new();
}
