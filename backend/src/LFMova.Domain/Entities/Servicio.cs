using LFMova.Domain.Enums;

namespace LFMova.Domain.Entities;

/// <summary>
/// Representa una operación concreta de transporte perteneciente a una
/// <c>Jornada</c>. No contiene <c>EmpresaId</c> propio: la empresa se obtiene
/// a través de la jornada. Sí contiene su propia <c>UnidadOperativaId</c>: la
/// asignación de unidad es individual por servicio, no por jornada, y puede
/// ser nula mientras el servicio no tenga unidad asignada durante la
/// planificación. No decide sus propias transiciones de estado ni su
/// asignación; eso corresponde a la capa de aplicación/reglas de dominio.
/// </summary>
public class Servicio
{
    /// <summary>Identificador único del servicio.</summary>
    public int ServicioId { get; set; }

    /// <summary>Jornada a la que pertenece el servicio. No cambia al reasignar la unidad operativa.</summary>
    public int JornadaId { get; set; }

    /// <summary>
    /// Unidad operativa asignada a este servicio. Es <c>null</c> mientras el
    /// servicio no tenga unidad asignada durante la planificación; no puede
    /// permanecer nula al publicarse.
    /// </summary>
    public int? UnidadOperativaId { get; set; }

    /// <summary>Sede asociada al servicio.</summary>
    public int SedeId { get; set; }

    /// <summary>Fecha calendario del servicio.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora programada del servicio.</summary>
    public TimeOnly HoraProgramada { get; set; }

    /// <summary>Sentido del transporte: ENTRADA (empleados → sede) o SALIDA (sede → empleados).</summary>
    public TipoServicio Tipo { get; set; }

    /// <summary>Estado actual del servicio dentro de su ciclo operativo.</summary>
    public EstadoServicio Estado { get; set; }

    /// <summary>Momento real en que el conductor inició el servicio. Nulo hasta que se inicia.</summary>
    public DateTime? HoraInicioReal { get; set; }

    /// <summary>Momento real en que el servicio finalizó. Nulo hasta que finaliza.</summary>
    public DateTime? HoraFinReal { get; set; }

    /// <summary>Latitud real del dispositivo del conductor al iniciar el servicio. Nula si no se pudo obtener.</summary>
    public double? LatitudInicio { get; set; }

    /// <summary>Longitud real del dispositivo del conductor al iniciar el servicio. Nula si no se pudo obtener.</summary>
    public double? LongitudInicio { get; set; }

    /// <summary>Latitud real del dispositivo del conductor al finalizar el servicio. Nula si no se pudo obtener.</summary>
    public double? LatitudFinalizacion { get; set; }

    /// <summary>Longitud real del dispositivo del conductor al finalizar el servicio. Nula si no se pudo obtener.</summary>
    public double? LongitudFinalizacion { get; set; }

    /// <summary>Jornada a la que pertenece el servicio.</summary>
    public Jornada? Jornada { get; set; }

    /// <summary>Unidad operativa asignada a este servicio, si existe.</summary>
    public UnidadOperativa? UnidadOperativa { get; set; }

    /// <summary>Sede asociada al servicio.</summary>
    public Sede? Sede { get; set; }

    /// <summary>Pasajeros que participan en este servicio.</summary>
    public ICollection<ServicioPasajero> ServiciosPasajero { get; set; } = new List<ServicioPasajero>();
}
