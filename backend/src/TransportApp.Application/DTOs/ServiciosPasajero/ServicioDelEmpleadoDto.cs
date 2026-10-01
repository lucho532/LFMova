using TransportApp.Domain.Enums;

namespace TransportApp.Application.DTOs.ServiciosPasajero;

/// <summary>
/// Representación pública de un servicio desde la perspectiva del empleado
/// que participa en él como pasajero. Combina datos del <c>Servicio</c> (que
/// no expone su propia empresa) con el estado de la participación del
/// empleado, para que el propio empleado pueda listar "mis servicios" sin
/// depender de una ruta anidada bajo una empresa que todavía no conoce.
/// </summary>
public class ServicioDelEmpleadoDto
{
    /// <summary>Identificador único de la participación del pasajero en el servicio.</summary>
    public int ServicioPasajeroId { get; set; }

    /// <summary>Empresa a la que pertenece el servicio.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Jornada a la que pertenece el servicio.</summary>
    public int JornadaId { get; set; }

    /// <summary>Identificador único del servicio.</summary>
    public int ServicioId { get; set; }

    /// <summary>Fecha calendario del servicio.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora programada del servicio.</summary>
    public TimeOnly HoraProgramada { get; set; }

    /// <summary>Sentido del transporte: ENTRADA (empleados → sede) o SALIDA (sede → empleados).</summary>
    public TipoServicio Tipo { get; set; }

    /// <summary>Estado actual del servicio dentro de su ciclo operativo.</summary>
    public EstadoServicio EstadoServicio { get; set; }

    /// <summary>Estado actual del propio empleado dentro del servicio.</summary>
    public EstadoServicioPasajero EstadoServicioPasajero { get; set; }

    /// <summary>Sede asociada al servicio.</summary>
    public int SedeId { get; set; }

    /// <summary>Dirección de recogida exacta utilizada para este servicio.</summary>
    public string DireccionRecogida { get; set; } = string.Empty;

    /// <summary>Momento (UTC) en que el conductor marcó "He llegado", o <c>null</c> si todavía no ha llegado.</summary>
    public DateTime? HoraLlegadaConductor { get; set; }

    /// <summary>
    /// Momento (UTC) en que se registró el resultado final del empleado (por
    /// ejemplo, "Recogido"), o <c>null</c> si aún no se procesa. Permite
    /// mostrarle al empleado que ya terminó su parte sin esperar a que el
    /// conductor finalice toda la ruta.
    /// </summary>
    public DateTime? HoraProcesado { get; set; }

    /// <summary>Nombre del conductor que recogerá al empleado, o <c>null</c> si el servicio aún no tiene unidad asignada.</summary>
    public string? ConductorNombre { get; set; }

    /// <summary>Placa del vehículo del conductor.</summary>
    public string? Placa { get; set; }

    /// <summary>Hora real en que el conductor inició la ruta (UTC), si ya inició.</summary>
    public DateTime? HoraInicioReal { get; set; }

    /// <summary>Hora real en que se finalizó la ruta (UTC), si ya finalizó.</summary>
    public DateTime? HoraFinReal { get; set; }
}
