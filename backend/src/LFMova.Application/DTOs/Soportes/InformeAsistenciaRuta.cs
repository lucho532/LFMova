using LFMova.Domain.Enums;

namespace LFMova.Application.DTOs.Soportes;

/// <summary>
/// Contenido de un informe de asistencia de una ruta: sus datos generales y los pasajeros incluidos, en
/// el orden en que se atendieron. Es lo que se entrega a la empresa; no cambia ningún registro.
/// </summary>
public class InformeAsistenciaRuta
{
    /// <summary>Empresa a la que pertenece la ruta.</summary>
    public string NombreEmpresa { get; set; } = string.Empty;

    /// <summary>Sentido del transporte (entrada o salida).</summary>
    public TipoServicio Tipo { get; set; }

    /// <summary>Sede de la ruta.</summary>
    public string NombreSede { get; set; } = string.Empty;

    /// <summary>Fecha de la ruta (hora de Colombia).</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora programada de la ruta (hora de Colombia).</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>Conductor que hizo la ruta, o <c>null</c> si no tenía.</summary>
    public string? NombreConductor { get; set; }

    /// <summary>Qué pasajeros se incluyeron.</summary>
    public FiltroAsistenciaRuta Filtro { get; set; }

    /// <summary>Cantidad total de pasajeros de la ruta, incluidos los que el filtro dejó fuera.</summary>
    public int TotalPasajerosRuta { get; set; }

    /// <summary>Pasajeros incluidos, en orden cronológico.</summary>
    public List<FilaInformeAsistencia> Filas { get; set; } = new();
}

/// <summary>Un pasajero dentro del informe de asistencia.</summary>
public class FilaInformeAsistencia
{
    /// <summary>Posición en el orden de recogida de la ruta; <c>null</c> si no llegó a procesarse (por ejemplo, avisó que no asistiría).</summary>
    public int? Numero { get; set; }

    /// <summary>Cédula del pasajero.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre completo del pasajero.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Estado final del pasajero en la ruta.</summary>
    public EstadoServicioPasajero Estado { get; set; }

    /// <summary>Hora de Colombia en que quedó registrado el resultado, o <c>null</c> si no se registró.</summary>
    public DateTime? HoraRegistro { get; set; }

    /// <summary>Segundos que esperó el conductor, de «He llegado» al resultado; <c>null</c> si falta alguno de los dos momentos.</summary>
    public int? EsperaSegundos { get; set; }
}
