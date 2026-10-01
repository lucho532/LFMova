namespace TransportApp.Application.DTOs.Zonas;

/// <summary>Representación pública de una zona.</summary>
public class ZonaDto
{
    /// <summary>Identificador único de la zona.</summary>
    public int ZonaId { get; set; }

    /// <summary>Identificador de la empresa a la que pertenece la zona.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Nombre de la zona.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Barrios que forman parte de esta zona.</summary>
    public List<string> Barrios { get; set; } = new();

    /// <summary>Indica si la zona está activa.</summary>
    public bool Activa { get; set; }

    /// <summary>Posición de la zona en la lista de la empresa.</summary>
    public int Orden { get; set; }

    /// <summary>Macrozona bajo la que se organiza esta zona, si tiene una asignada.</summary>
    public int? MacroZonaId { get; set; }

    /// <summary>Nombre de la macrozona, si tiene una asignada (solo para mostrarla sin otra consulta).</summary>
    public string? MacroZonaNombre { get; set; }

    /// <summary>Corredor vial al que pertenece esta zona, si tiene uno asignado.</summary>
    public int? CorredorVialId { get; set; }

    /// <summary>Nombre del corredor vial, si tiene uno asignado (solo para mostrarlo sin otra consulta).</summary>
    public string? CorredorVialNombre { get; set; }
}
