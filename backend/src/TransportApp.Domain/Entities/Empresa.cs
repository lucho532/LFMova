namespace TransportApp.Domain.Entities;

/// <summary>
/// Representa una empresa cliente de la plataforma de transporte.
/// Su responsabilidad es mantener la identidad básica y el estado de
/// activación de la empresa.
/// No contiene sedes, empleados, conductores ni ninguna otra relación
/// operativa: esas asociaciones se modelan desde las entidades
/// correspondientes. No debe contener lógica de negocio ni de autorización
/// (por ejemplo, quién puede crear o administrar una empresa).
/// </summary>
public class Empresa
{
    /// <summary>Identificador único de la empresa.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Nombre de la empresa.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Identificador fiscal de la empresa (CIF/NIT). Único en toda la
    /// plataforma.
    /// </summary>
    public string Cif { get; set; } = string.Empty;

    /// <summary>Dirección física de la empresa.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>
    /// Indica si la empresa está activa. Una empresa inactiva conserva su
    /// información histórica y no se elimina físicamente.
    /// </summary>
    public bool Activa { get; set; }

    /// <summary>Sedes de la empresa.</summary>
    public ICollection<Sede> Sedes { get; set; } = new List<Sede>();

    /// <summary>Zonas geográficas de recogida definidas por la empresa.</summary>
    public ICollection<Zona> Zonas { get; set; } = new List<Zona>();

    /// <summary>Macrozonas (comunas) bajo las que se organizan las zonas de la empresa.</summary>
    public ICollection<MacroZona> MacroZonas { get; set; } = new List<MacroZona>();

    /// <summary>Barreras geográficas declaradas entre barrios de la empresa.</summary>
    public ICollection<BarreraGeografica> BarrerasGeograficas { get; set; } = new List<BarreraGeografica>();

    /// <summary>Corredores viales (agrupaciones de zonas que comparten vehículo) de la empresa.</summary>
    public ICollection<CorredorVial> CorredoresViales { get; set; } = new List<CorredorVial>();

    /// <summary>Empleados que pertenecen actualmente a la empresa.</summary>
    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();

    /// <summary>Importaciones de Excel realizadas para la empresa.</summary>
    public ICollection<ImportacionExcel> ImportacionesExcel { get; set; } = new List<ImportacionExcel>();

    /// <summary>Programaciones de transporte generadas para la empresa.</summary>
    public ICollection<ProgramacionTransporte> ProgramacionesTransporte { get; set; } = new List<ProgramacionTransporte>();

    /// <summary>Jornadas de la empresa.</summary>
    public ICollection<Jornada> Jornadas { get; set; } = new List<Jornada>();

    /// <summary>Vinculaciones con conductores autorizados a trabajar para la empresa.</summary>
    public ICollection<VinculacionConductorEmpresa> VinculacionesConductorEmpresa { get; set; } = new List<VinculacionConductorEmpresa>();
}
