namespace LFMova.Application.DTOs.Empleados;

/// <summary>Representación pública de un empleado.</summary>
public class EmpleadoDto
{
    /// <summary>Identificador único del perfil de empleado.</summary>
    public int EmpleadoId { get; set; }

    /// <summary>Identificador del usuario que representa a esta persona.</summary>
    public int UsuarioId { get; set; }

    /// <summary>Identificador de la empresa a la que pertenece actualmente el empleado.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Cédula del empleado.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Correo de la cuenta del empleado, o <c>null</c> si todavía no la ha activado (por ejemplo, creada por importación de Excel).</summary>
    public string? Email { get; set; }

    /// <summary>Nombre completo del empleado.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto del empleado.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Dirección actual/habitual del empleado.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Barrio actual/habitual del empleado.</summary>
    public string Barrio { get; set; } = string.Empty;

    /// <summary>Indica si el perfil de empleado está activo.</summary>
    public bool Activo { get; set; }
}
