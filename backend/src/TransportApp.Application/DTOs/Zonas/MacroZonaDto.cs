namespace TransportApp.Application.DTOs.Zonas;

/// <summary>Representación pública de una macrozona.</summary>
public class MacroZonaDto
{
    /// <summary>Identificador único de la macrozona.</summary>
    public int MacroZonaId { get; set; }

    /// <summary>Identificador de la empresa a la que pertenece la macrozona.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Nombre de la macrozona.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Indica si la macrozona está activa.</summary>
    public bool Activa { get; set; }
}
