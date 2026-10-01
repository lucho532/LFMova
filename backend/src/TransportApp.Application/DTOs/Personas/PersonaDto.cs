namespace TransportApp.Application.DTOs.Personas;

/// <summary>
/// Datos de una persona registrada en la plataforma, devueltos a un
/// administrador o coordinador que la busca por cédula para confirmar su
/// identidad antes de asignarle un rol. Es solo de lectura y no revela con
/// qué empresas está relacionada la persona.
/// </summary>
public class PersonaDto
{
    /// <summary>Cédula de la persona.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre completo de la persona.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Correo electrónico de la cuenta, si lo tiene.</summary>
    public string? Email { get; set; }

    /// <summary>Teléfono de contacto.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Indica si la persona ya tiene el rol de conductor activo.</summary>
    public bool EsConductor { get; set; }

    /// <summary>Indica si la persona ya es coordinador activo de alguna empresa.</summary>
    public bool EsCoordinador { get; set; }

    /// <summary>Empresa de la que es coordinadora, o <c>null</c> si no coordina ninguna.</summary>
    public EmpresaResumenDto? EmpresaCoordinada { get; set; }

    /// <summary>Empresas con las que tiene una vinculación activa como conductor.</summary>
    public List<EmpresaResumenDto> EmpresasConductor { get; set; } = new();

    /// <summary>Placas de sus vehículos activos (si es conductor).</summary>
    public List<string> Placas { get; set; } = new();

    /// <summary>Empresa a la que pertenece como empleado, o <c>null</c> si no tiene.</summary>
    public EmpresaResumenDto? EmpresaEmpleado { get; set; }
}

/// <summary>Identificador y nombre de una empresa.</summary>
public class EmpresaResumenDto
{
    /// <summary>Identificador de la empresa.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Nombre de la empresa.</summary>
    public string Nombre { get; set; } = string.Empty;
}
