using LFMova.Domain.Enums;

namespace LFMova.Application.DTOs.Importaciones;

/// <summary>
/// Datos para que el coordinador cree una ruta vacía a mano para una unidad
/// operativa (por ejemplo, para dividir una ruta sobrecargada moviéndole
/// pasajeros después): fecha, hora, tipo, sede y unidad la identifican; si ya
/// existe una ruta con esos mismos datos para esa unidad, se reutiliza en vez
/// de crear otra.
/// </summary>
public class CrearRutaVaciaDto
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
}
