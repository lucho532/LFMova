namespace LFMova.Application.DTOs.Sedes;

/// <summary>Representación pública de una sede.</summary>
public class SedeDto
{
    /// <summary>Identificador único de la sede.</summary>
    public int SedeId { get; set; }

    /// <summary>Identificador de la empresa a la que pertenece la sede.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Nombre de la sede.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Dirección de la sede.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Ciudad donde se ubica la sede.</summary>
    public string Ciudad { get; set; } = string.Empty;

    /// <summary>Barrio donde se ubica la sede.</summary>
    public string Barrio { get; set; } = string.Empty;

    /// <summary>Latitud de la sede, si está disponible.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud de la sede, si está disponible.</summary>
    public double? Longitud { get; set; }

    /// <summary>Indica si la sede está activa.</summary>
    public bool Activa { get; set; }
}
