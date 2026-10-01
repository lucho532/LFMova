using TransportApp.Domain.Enums;

namespace TransportApp.Application.DTOs.ServiciosPasajero;

/// <summary>Representación pública de la participación de un empleado en un servicio.</summary>
public class ServicioPasajeroDto
{
    /// <summary>Identificador único de la participación del pasajero en el servicio.</summary>
    public int ServicioPasajeroId { get; set; }

    /// <summary>Servicio en el que participa el pasajero.</summary>
    public int ServicioId { get; set; }

    /// <summary>Programación de transporte que originó esta participación.</summary>
    public int ProgramacionTransporteId { get; set; }

    /// <summary>Empleado que participa en el servicio.</summary>
    public int EmpleadoId { get; set; }

    /// <summary>Estado actual del pasajero dentro del servicio.</summary>
    public EstadoServicioPasajero Estado { get; set; }

    /// <summary>Orden operativo de recogida/entrega del pasajero.</summary>
    public int Orden { get; set; }

    /// <summary>Momento (UTC) en que el conductor marcó "He llegado", o <c>null</c> si todavía no ha llegado.</summary>
    public DateTime? HoraLlegadaConductor { get; set; }

    /// <summary>Momento (UTC) en que se registró el resultado final del pasajero (por ejemplo, "Recogido"), o <c>null</c> si aún no se procesa.</summary>
    public DateTime? HoraProcesado { get; set; }

    /// <summary>Dirección de recogida exacta utilizada para este servicio.</summary>
    public string DireccionRecogida { get; set; } = string.Empty;

    /// <summary>Latitud utilizada para este servicio, si está disponible.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud utilizada para este servicio, si está disponible.</summary>
    public double? Longitud { get; set; }

    /// <summary>
    /// Nombre completo del empleado. Se incluye para que el conductor pueda
    /// identificarlo durante la ejecución del servicio (ver <c>spec.md</c>
    /// §24).
    /// </summary>
    public string NombreCompletoEmpleado { get; set; } = string.Empty;

    /// <summary>
    /// Teléfono de contacto del empleado. Se incluye para que el conductor
    /// pueda llamarlo mediante las capacidades de su dispositivo (ver
    /// <c>tasks.md</c> T086); la aplicación no graba llamadas.
    /// </summary>
    public string TelefonoEmpleado { get; set; } = string.Empty;

    /// <summary>Cédula del empleado, para identificarlo si no se logra contactarlo desde la app.</summary>
    public string CedulaEmpleado { get; set; } = string.Empty;

    /// <summary>Barrio habitual del empleado.</summary>
    public string BarrioEmpleado { get; set; } = string.Empty;
}
