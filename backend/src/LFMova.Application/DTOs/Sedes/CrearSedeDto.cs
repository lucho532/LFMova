namespace LFMova.Application.DTOs.Sedes;

/// <summary>
/// Datos de entrada para crear una sede. Las coordenadas son opcionales: no
/// se bloquea la creación cuando el mecanismo de geocodificación todavía no
/// está integrado.
/// </summary>
public class CrearSedeDto
{
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
}
