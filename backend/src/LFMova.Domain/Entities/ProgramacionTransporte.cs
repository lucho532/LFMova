using LFMova.Domain.Enums;

namespace LFMova.Domain.Entities;

/// <summary>
/// Representa la necesidad concreta de transporte de un empleado para una
/// fecha y hora determinada. Puede existir sin estar todavía asignada a un
/// <c>ServicioPasajero</c>. Conserva su <c>EmpresaId</c> como contexto
/// histórico, independientemente de la empresa actual del empleado.
/// No decide su propia asignación a un servicio; eso corresponde a la capa
/// de aplicación.
/// </summary>
public class ProgramacionTransporte
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

    /// <summary>Dirección de recogida del empleado para esta programación.</summary>
    public string DireccionRecogida { get; set; } = string.Empty;

    /// <summary>Barrio de recogida del empleado para esta programación.</summary>
    public string BarrioRecogida { get; set; } = string.Empty;

    /// <summary>Empresa para la que se generó la programación (contexto histórico).</summary>
    public Empresa? Empresa { get; set; }

    /// <summary>Empleado para el que se generó la programación.</summary>
    public Empleado? Empleado { get; set; }

    /// <summary>Sede asociada a la programación.</summary>
    public Sede? Sede { get; set; }

    /// <summary>Servicio pasajero en el que se concretó esta programación, si existe.</summary>
    public ServicioPasajero? ServicioPasajero { get; set; }
}
