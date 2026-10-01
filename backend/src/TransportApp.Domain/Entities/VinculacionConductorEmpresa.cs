namespace TransportApp.Domain.Entities;

/// <summary>
/// Representa la autorización de un <c>Conductor</c> para trabajar con una
/// empresa determinada. La combinación de <c>ConductorId</c> y
/// <c>EmpresaId</c> debe ser única (restricción a definir a nivel de base de
/// datos en la tarea de configuración correspondiente).
/// No decide si el conductor puede ejecutar servicios de esa empresa; esa
/// autorización operativa corresponde a la capa de aplicación.
/// </summary>
public class VinculacionConductorEmpresa
{
    /// <summary>Identificador único de la vinculación.</summary>
    public int VinculacionConductorEmpresaId { get; set; }

    /// <summary>Identificador del conductor vinculado.</summary>
    public int ConductorId { get; set; }

    /// <summary>Identificador de la empresa con la que se vincula el conductor.</summary>
    public int EmpresaId { get; set; }

    /// <summary>
    /// Indica si la vinculación está activa. Es independiente del estado
    /// global del conductor (<c>Conductor.Activo</c>).
    /// </summary>
    public bool Activa { get; set; }

    /// <summary>Conductor vinculado.</summary>
    public Conductor? Conductor { get; set; }

    /// <summary>Empresa con la que se vincula el conductor.</summary>
    public Empresa? Empresa { get; set; }
}
