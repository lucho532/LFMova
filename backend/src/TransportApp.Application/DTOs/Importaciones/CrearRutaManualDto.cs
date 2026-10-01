using TransportApp.Domain.Enums;

namespace TransportApp.Application.DTOs.Importaciones;

/// <summary>
/// Datos para que el coordinador cree una ruta a mano (sin Excel): fecha,
/// hora, tipo, sede y unidad operativa la identifican; si ya existe una ruta
/// con esos mismos datos para esa unidad, el pasajero se agrega ahí en lugar
/// de crear una nueva.
/// </summary>
public class CrearRutaManualDto
{
    /// <summary>Fecha operativa de la ruta.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora programada.</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>Entrada o salida.</summary>
    public TipoServicio Tipo { get; set; }

    /// <summary>Sede de la ruta.</summary>
    public int SedeId { get; set; }

    /// <summary>Unidad operativa (conductor + vehículo) que hará la ruta.</summary>
    public int UnidadOperativaId { get; set; }

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
