using TransportApp.Domain.Enums;

namespace TransportApp.Application.DTOs.Programaciones;

/// <summary>Representación pública de una programación de transporte.</summary>
public class ProgramacionDto
{
    /// <summary>Identificador único de la programación.</summary>
    public int ProgramacionTransporteId { get; set; }

    /// <summary>Empresa para la que se generó la programación (contexto histórico).</summary>
    public int EmpresaId { get; set; }

    /// <summary>Empleado para el que se generó la programación.</summary>
    public int EmpleadoId { get; set; }

    /// <summary>Sede asociada a la programación.</summary>
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
