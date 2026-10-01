namespace TransportApp.Application.DTOs.Importaciones;

/// <summary>
/// Datos para agregar a mano un empleado que no venía en el Excel y asignarlo
/// a una ruta (servicio) ya creada de la jornada.
/// </summary>
public class AgregarEmpleadoManualDto
{
    /// <summary>Ruta (servicio) a la que se asigna el empleado.</summary>
    public int ServicioId { get; set; }

    /// <summary>Cédula del empleado.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre completo.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Celular.</summary>
    public string Celular { get; set; } = string.Empty;

    /// <summary>Dirección de recogida o de destino.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Barrio.</summary>
    public string Barrio { get; set; } = string.Empty;
}
