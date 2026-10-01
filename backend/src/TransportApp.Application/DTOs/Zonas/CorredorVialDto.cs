namespace TransportApp.Application.DTOs.Zonas;

/// <summary>Representación pública de un corredor vial.</summary>
public class CorredorVialDto
{
    /// <summary>Identificador único del corredor vial.</summary>
    public int CorredorVialId { get; set; }

    /// <summary>Identificador de la empresa a la que pertenece el corredor.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Nombre del corredor vial.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Indica si el corredor está activo.</summary>
    public bool Activo { get; set; }
}
