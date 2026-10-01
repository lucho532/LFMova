namespace LFMova.Application.DTOs.Empleados;

/// <summary>
/// Datos actuales del empleado que un coordinador puede actualizar. No
/// incluye la empresa: el cambio de empresa de un empleado se resuelve
/// exclusivamente mediante la importación de Excel (ver <c>spec.md</c> §39).
/// </summary>
public class ActualizarEmpleadoDto
{
    /// <summary>Nombre completo del empleado.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto del empleado.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Dirección actual/habitual del empleado.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Barrio actual/habitual del empleado.</summary>
    public string Barrio { get; set; } = string.Empty;
}
