using TransportApp.Domain.Enums;

namespace TransportApp.Application.DTOs.Servicios;

/// <summary>Representación pública de un servicio.</summary>
public class ServicioDto
{
    /// <summary>Identificador único del servicio.</summary>
    public int ServicioId { get; set; }

    /// <summary>Empresa a la que pertenece el servicio (a través de su jornada).</summary>
    public int EmpresaId { get; set; }

    /// <summary>Jornada a la que pertenece el servicio.</summary>
    public int JornadaId { get; set; }

    /// <summary>
    /// Unidad operativa asignada al servicio. Es <c>null</c> mientras no
    /// tenga unidad asignada durante la planificación.
    /// </summary>
    public int? UnidadOperativaId { get; set; }

    /// <summary>Sede asociada al servicio.</summary>
    public int SedeId { get; set; }

    /// <summary>Nombre de la sede asociada al servicio.</summary>
    public string NombreSede { get; set; } = string.Empty;

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

    /// <summary>Latitud real del dispositivo del conductor al iniciar, o <c>null</c> si no se pudo obtener.</summary>
    public double? LatitudInicio { get; set; }

    /// <summary>Longitud real del dispositivo del conductor al iniciar, o <c>null</c> si no se pudo obtener.</summary>
    public double? LongitudInicio { get; set; }

    /// <summary>Latitud real del dispositivo del conductor al finalizar, o <c>null</c> si no se pudo obtener.</summary>
    public double? LatitudFinalizacion { get; set; }

    /// <summary>Longitud real del dispositivo del conductor al finalizar, o <c>null</c> si no se pudo obtener.</summary>
    public double? LongitudFinalizacion { get; set; }

    /// <summary>Cantidad de pasajeros asignados a este servicio.</summary>
    public int CantidadPasajeros { get; set; }
}
