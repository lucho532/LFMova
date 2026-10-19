namespace LFMova.Domain.Entities;

/// <summary>
/// Representa el perfil de conductor asociado a un <c>Usuario</c>. Un
/// conductor es independiente de una empresa concreta y puede trabajar para
/// varias empresas mediante <c>VinculacionConductorEmpresa</c>.
/// No decide con qué empresas está vinculado ni qué vehículos utiliza: esas
/// relaciones se modelan desde las entidades correspondientes. No contiene
/// lógica de negocio.
/// </summary>
public class Conductor
{
    /// <summary>Identificador único del perfil de conductor.</summary>
    public int ConductorId { get; set; }

    /// <summary>Identificador del <c>Usuario</c> que representa a esta persona.</summary>
    public int UsuarioId { get; set; }

    /// <summary>Nombre completo del conductor.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto del conductor.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>
    /// Indica si el conductor está activo globalmente. Es independiente del
    /// estado de su vinculación con cada empresa (<c>VinculacionConductorEmpresa.Activa</c>).
    /// </summary>
    public bool Activo { get; set; }

    /// <summary>Usuario que representa a esta persona.</summary>
    public Usuario? Usuario { get; set; }

    /// <summary>Empresas con las que este conductor tiene una vinculación.</summary>
    public ICollection<VinculacionConductorEmpresa> VinculacionesConductorEmpresa { get; set; } = new List<VinculacionConductorEmpresa>();

    /// <summary>Vehículos propiedad de este conductor.</summary>
    public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();

    /// <summary>Unidades operativas formadas por este conductor.</summary>
    public ICollection<UnidadOperativa> UnidadesOperativas { get; set; } = new List<UnidadOperativa>();
}
