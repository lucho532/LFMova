namespace LFMova.Application.DTOs.Zonas;

/// <summary>Datos de entrada para actualizar el nombre y los barrios de una zona.</summary>
public class ActualizarZonaDto
{
    /// <summary>Nombre de la zona.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Barrios que forman parte de esta zona.</summary>
    public List<string> Barrios { get; set; } = new();

    /// <summary>Macrozona bajo la que se organiza esta zona, si se quiere clasificar (opcional).</summary>
    public int? MacroZonaId { get; set; }

    /// <summary>Corredor vial al que pertenece esta zona, si se quiere clasificar (opcional).</summary>
    public int? CorredorVialId { get; set; }
}
