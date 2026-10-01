using TransportApp.Domain.Enums;

namespace TransportApp.Application.DTOs.Servicios;

/// <summary>Datos necesarios para crear un servicio asociado a una jornada.</summary>
public class CrearServicioDto
{
    /// <summary>
    /// Identificador de la unidad operativa a asignar al crear el servicio.
    /// Opcional: puede quedar sin asignar (<c>null</c>) durante la
    /// planificación y asignarse después mediante <c>AsignarUnidadAsync</c>.
    /// </summary>
    public int? UnidadOperativaId { get; set; }

    /// <summary>Identificador de la sede asociada al servicio.</summary>
    public int SedeId { get; set; }

    /// <summary>Fecha calendario del servicio.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora programada del servicio.</summary>
    public TimeOnly HoraProgramada { get; set; }

    /// <summary>Sentido del transporte: ENTRADA (empleados → sede) o SALIDA (sede → empleados).</summary>
    public TipoServicio Tipo { get; set; }
}
