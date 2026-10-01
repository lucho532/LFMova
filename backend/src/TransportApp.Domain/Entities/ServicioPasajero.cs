using TransportApp.Domain.Enums;

namespace TransportApp.Domain.Entities;

/// <summary>
/// Representa la participación de un empleado concreto en un
/// <c>Servicio</c>. Conserva la dirección exacta utilizada para ese
/// servicio, independiente de la dirección actual del empleado. Una
/// <c>ProgramacionTransporte</c> puede estar asociada como máximo a un
/// <c>ServicioPasajero</c> (restricción única sobre
/// <c>ProgramacionTransporteId</c>, a definir a nivel de base de datos).
/// No decide su propio estado ni orden de recogida; esas operaciones
/// corresponden a la capa de aplicación.
/// </summary>
public class ServicioPasajero
{
    /// <summary>Identificador único de la participación del pasajero en el servicio.</summary>
    public int ServicioPasajeroId { get; set; }

    /// <summary>Servicio en el que participa el pasajero.</summary>
    public int ServicioId { get; set; }

    /// <summary>
    /// Programación de transporte que originó esta participación. Única por
    /// cada <c>ServicioPasajero</c>.
    /// </summary>
    public int ProgramacionTransporteId { get; set; }

    /// <summary>Empleado que participa en el servicio.</summary>
    public int EmpleadoId { get; set; }

    /// <summary>Estado actual del pasajero dentro del servicio.</summary>
    public EstadoServicioPasajero Estado { get; set; }

    /// <summary>Orden operativo de recogida/entrega del pasajero. Puede ser modificado por el conductor.</summary>
    public int Orden { get; set; }

    /// <summary>
    /// Momento (UTC) en que el conductor marcó "He llegado" para este
    /// pasajero. Es <c>null</c> hasta que eso ocurre. Es la referencia que
    /// usan tanto el conductor como el empleado para mostrar el mismo
    /// cronómetro de espera, en vez de cada dispositivo contar por su cuenta.
    /// </summary>
    public DateTime? HoraLlegadaConductor { get; set; }

    /// <summary>
    /// Momento (UTC) en que el conductor registró el resultado final de este
    /// pasajero (por ejemplo, "Recogido"). Es <c>null</c> hasta que eso
    /// ocurre. Permite mostrarle al coordinador a qué hora se recogió cada
    /// pasajero, no solo el estado actual.
    /// </summary>
    public DateTime? HoraProcesado { get; set; }

    /// <summary>
    /// Dirección de recogida exacta utilizada para este servicio. Es
    /// histórica e inmutable frente a cambios posteriores del empleado.
    /// </summary>
    public string DireccionRecogida { get; set; } = string.Empty;

    /// <summary>Latitud utilizada para este servicio. Puede ser <c>null</c> si no está disponible.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud utilizada para este servicio. Puede ser <c>null</c> si no está disponible.</summary>
    public double? Longitud { get; set; }

    /// <summary>Servicio en el que participa el pasajero.</summary>
    public Servicio? Servicio { get; set; }

    /// <summary>Programación de transporte que originó esta participación.</summary>
    public ProgramacionTransporte? ProgramacionTransporte { get; set; }

    /// <summary>Empleado que participa en el servicio.</summary>
    public Empleado? Empleado { get; set; }

    /// <summary>Incidencias registradas sobre este pasajero durante el servicio.</summary>
    public ICollection<Incidencia> Incidencias { get; set; } = new List<Incidencia>();

    /// <summary>Conversación privada entre el conductor y el empleado para este servicio, si existe.</summary>
    public Conversacion? Conversacion { get; set; }
}
