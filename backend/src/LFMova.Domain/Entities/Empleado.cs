namespace LFMova.Domain.Entities;

/// <summary>
/// Representa el perfil laboral/operativo de empleado asociado a un
/// <c>Usuario</c>. Su responsabilidad es mantener los datos operativos del
/// empleado (nombre, contacto, dirección) y la empresa a la que pertenece
/// actualmente.
/// No es una identidad independiente: la identidad de la persona y su cédula
/// pertenecen exclusivamente a <c>Usuario</c>. No decide ni ejecuta el
/// cambio de empresa, ni ninguna otra regla de negocio; esas operaciones
/// corresponden a la capa de aplicación.
/// </summary>
public class Empleado
{
    /// <summary>Identificador único del perfil de empleado.</summary>
    public int EmpleadoId { get; set; }

    /// <summary>Identificador del <c>Usuario</c> que representa a esta persona.</summary>
    public int UsuarioId { get; set; }

    /// <summary>Identificador de la empresa a la que pertenece actualmente el empleado.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Nombre completo del empleado.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto del empleado.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Dirección actual/habitual del empleado.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Barrio actual/habitual del empleado.</summary>
    public string Barrio { get; set; } = string.Empty;

    /// <summary>
    /// Indica si el perfil de empleado está activo. Permite desactivarlo sin
    /// eliminar el registro ni su información histórica.
    /// </summary>
    public bool Activo { get; set; }

    /// <summary>Usuario que representa a esta persona.</summary>
    public Usuario? Usuario { get; set; }

    /// <summary>Empresa a la que pertenece actualmente el empleado.</summary>
    public Empresa? Empresa { get; set; }

    /// <summary>Programaciones de transporte generadas para este empleado.</summary>
    public ICollection<ProgramacionTransporte> ProgramacionesTransporte { get; set; } = new List<ProgramacionTransporte>();

    /// <summary>Participaciones de este empleado en servicios.</summary>
    public ICollection<ServicioPasajero> ServiciosPasajero { get; set; } = new List<ServicioPasajero>();

    /// <summary>Ubicaciones de recogida históricas de este empleado.</summary>
    public ICollection<UbicacionRecogidaHistorica> UbicacionesRecogidaHistorica { get; set; } = new List<UbicacionRecogidaHistorica>();
}
