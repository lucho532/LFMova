namespace TransportApp.Application.DTOs.Conductores;

/// <summary>Representación pública de una vinculación entre un conductor y una empresa.</summary>
public class VinculacionConductorEmpresaDto
{
    /// <summary>Identificador único de la vinculación.</summary>
    public int VinculacionConductorEmpresaId { get; set; }

    /// <summary>Identificador del conductor vinculado.</summary>
    public int ConductorId { get; set; }

    /// <summary>Identificador de la empresa con la que se vincula el conductor.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Indica si la vinculación está activa.</summary>
    public bool Activa { get; set; }
}
