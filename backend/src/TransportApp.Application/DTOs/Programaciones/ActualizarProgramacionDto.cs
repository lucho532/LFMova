using TransportApp.Domain.Enums;

namespace TransportApp.Application.DTOs.Programaciones;

/// <summary>
/// Datos actualizables de una programación de transporte. No incluye el
/// empleado: una programación pertenece siempre al empleado para el que fue
/// creada.
/// </summary>
public class ActualizarProgramacionDto
{
    /// <summary>Identificador de la sede asociada a la programación.</summary>
    public int SedeId { get; set; }

    /// <summary>Fecha calendario de la programación.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora programada del transporte.</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>Sentido del transporte: ENTRADA (empleado → sede) o SALIDA (sede → empleado).</summary>
    public TipoServicio Tipo { get; set; }

    /// <summary>Dirección de recogida/entrega del empleado para esta programación.</summary>
    public string DireccionRecogida { get; set; } = string.Empty;

    /// <summary>Barrio de recogida/entrega del empleado para esta programación.</summary>
    public string BarrioRecogida { get; set; } = string.Empty;
}
