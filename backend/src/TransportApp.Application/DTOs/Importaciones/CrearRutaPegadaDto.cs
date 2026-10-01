using TransportApp.Domain.Enums;

namespace TransportApp.Application.DTOs.Importaciones;

/// <summary>
/// Datos para crear una ruta a partir de filas de pasajeros pegadas desde un
/// Excel externo: fecha, hora, tipo y sede identifican la ruta (siempre se
/// crea una nueva, sin reutilizar otra existente). La unidad operativa es
/// opcional: si se indica, la ruta queda asignada a ese conductor de una vez;
/// si no, queda sin conductor (<c>PENDIENTE_ASIGNACION</c>) para asignárselo
/// después desde la tarjeta, igual que una ruta importada que no alcanzó a
/// repartirse por falta de unidades.
/// </summary>
public class CrearRutaPegadaDto
{
    /// <summary>Fecha operativa de la ruta.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora programada.</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>Entrada o salida.</summary>
    public TipoServicio Tipo { get; set; }

    /// <summary>Sede de la ruta.</summary>
    public int SedeId { get; set; }

    /// <summary>Unidad operativa (conductor + vehículo) a asignar de una vez, o <c>null</c> para dejarla sin asignar.</summary>
    public int? UnidadOperativaId { get; set; }

    /// <summary>Pasajeros ya interpretados (el pegado y el orden de columnas se resuelven en el frontend).</summary>
    public List<FilaPasajeroPegadoDto> Pasajeros { get; set; } = new();
}
