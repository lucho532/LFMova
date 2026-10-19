namespace LFMova.Application.DTOs.Conductores;

/// <summary>Representación pública de un conductor.</summary>
public class ConductorDto
{
    /// <summary>Identificador único del conductor.</summary>
    public int ConductorId { get; set; }

    /// <summary>Identificador del usuario que representa a esta persona.</summary>
    public int UsuarioId { get; set; }

    /// <summary>Cédula del conductor.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre completo del conductor.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto del conductor.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Indica si el conductor está activo globalmente.</summary>
    public bool Activo { get; set; }

    /// <summary>
    /// Identificadores de las empresas con las que el conductor tiene una
    /// vinculación actualmente activa.
    /// </summary>
    public List<int> EmpresaIdsVinculadosActivos { get; set; } = new();
}
