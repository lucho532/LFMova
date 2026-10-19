using LFMova.Domain.Enums;

namespace LFMova.Application.DTOs.Soportes;

/// <summary>
/// Un pasajero de una ruta de la empresa tal como lo entrega la consulta de lectura, con los datos de
/// su ruta y de su conductor. Es la materia prima del informe de asistencia por período: el servicio decide
/// cuáles entran y en qué orden. Los instantes reales (<see cref="HoraLlegadaConductor"/>,
/// <see cref="HoraProcesado"/>) llegan en UTC.
/// </summary>
public class PasajeroDeRutaDto
{
    /// <summary>Fecha de la ruta (hora de Colombia).</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora programada de la ruta (hora de Colombia).</summary>
    public TimeOnly HoraRuta { get; set; }

    /// <summary>Identificador de la ruta (servicio).</summary>
    public int ServicioId { get; set; }

    /// <summary>Jornada a la que pertenece la ruta.</summary>
    public int JornadaId { get; set; }

    /// <summary>Sentido de la ruta.</summary>
    public TipoServicio Tipo { get; set; }

    /// <summary>Nombre de la sede de la ruta.</summary>
    public string Sede { get; set; } = string.Empty;

    /// <summary>Conductor asignado a la ruta, o <c>null</c> si no tenía unidad.</summary>
    public string? Conductor { get; set; }

    /// <summary>Placa del vehículo de la unidad asignada, o <c>null</c> si no tenía unidad.</summary>
    public string? Placa { get; set; }

    /// <summary>Cédula del pasajero.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre completo del pasajero.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto del pasajero.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Dirección de recogida que quedó guardada para esa ruta (no la actual del empleado).</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Barrio del pasajero.</summary>
    public string Barrio { get; set; } = string.Empty;

    /// <summary>Estado del pasajero en la ruta.</summary>
    public EstadoServicioPasajero Estado { get; set; }

    /// <summary>Orden de recogida planeado.</summary>
    public int Orden { get; set; }

    /// <summary>Instante (UTC) en que el conductor marcó «He llegado», o <c>null</c>.</summary>
    public DateTime? HoraLlegadaConductor { get; set; }

    /// <summary>Instante (UTC) en que quedó registrado el resultado, o <c>null</c>.</summary>
    public DateTime? HoraProcesado { get; set; }
}

/// <summary>
/// Contenido del informe de asistencia de varias rutas (una jornada o un rango de días): los pasajeros
/// que cumplen el filtro con su ruta, conductor y horarios. Es lo que se entrega a la empresa; no cambia
/// ningún registro.
/// </summary>
public class InformeAsistenciaPeriodo
{
    /// <summary>Empresa del informe.</summary>
    public string NombreEmpresa { get; set; } = string.Empty;

    /// <summary>Primer día cubierto.</summary>
    public DateOnly Desde { get; set; }

    /// <summary>Último día cubierto.</summary>
    public DateOnly Hasta { get; set; }

    /// <summary>Qué pasajeros se incluyeron.</summary>
    public FiltroAsistenciaRuta Filtro { get; set; }

    /// <summary>Pasajeros incluidos, por fecha y hora de ruta y luego en orden cronológico.</summary>
    public List<FilaInformeAsistenciaPeriodo> Filas { get; set; } = new();
}

/// <summary>Un pasajero dentro del informe de asistencia por período.</summary>
public class FilaInformeAsistenciaPeriodo
{
    /// <summary>Ruta (servicio) del pasajero; junto con <see cref="JornadaId"/> permite abrir su ficha completa.</summary>
    public int ServicioId { get; set; }

    /// <summary>Jornada de la ruta del pasajero.</summary>
    public int JornadaId { get; set; }

    /// <summary>Fecha de la ruta.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora programada de la ruta.</summary>
    public TimeOnly HoraRuta { get; set; }

    /// <summary>Sentido de la ruta.</summary>
    public TipoServicio Tipo { get; set; }

    /// <summary>Sede de la ruta.</summary>
    public string Sede { get; set; } = string.Empty;

    /// <summary>Conductor que tenía asignado la ruta, o <c>null</c> si no tenía.</summary>
    public string? Conductor { get; set; }

    /// <summary>Placa del vehículo de la ruta, o <c>null</c> si no tenía unidad.</summary>
    public string? Placa { get; set; }

    /// <summary>Cédula del pasajero.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre completo del pasajero.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Dirección de recogida de esa ruta.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Barrio del pasajero.</summary>
    public string Barrio { get; set; } = string.Empty;

    /// <summary>Estado final del pasajero en la ruta.</summary>
    public EstadoServicioPasajero Estado { get; set; }

    /// <summary>Hora de Colombia en que quedó registrado el resultado, o <c>null</c>.</summary>
    public DateTime? HoraRegistro { get; set; }

    /// <summary>Segundos de espera del conductor (de «He llegado» al resultado), o <c>null</c> si falta alguno de los dos momentos.</summary>
    public int? EsperaSegundos { get; set; }
}
