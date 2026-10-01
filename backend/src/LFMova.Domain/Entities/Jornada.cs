namespace LFMova.Domain.Entities;

/// <summary>
/// Representa el bloque operativo de transporte de una empresa para una
/// fecha operativa determinada. No tiene hora de inicio ni de finalización:
/// <c>FechaOperativa</c> es una fecha de referencia y no necesariamente
/// coincide con la fecha calendario de todos los servicios que contiene
/// (puede haber servicios que crucen medianoche). No pertenece a una única
/// <c>UnidadOperativa</c>: cada <c>Servicio</c> se asigna individualmente a
/// la suya (ver <c>Servicio.UnidadOperativaId</c>). No decide qué servicios
/// contiene ni su publicación; eso corresponde a la capa de aplicación.
/// </summary>
public class Jornada
{
    /// <summary>Identificador único de la jornada.</summary>
    public int JornadaId { get; set; }

    /// <summary>Empresa a la que pertenece la jornada.</summary>
    public int EmpresaId { get; set; }

    /// <summary>
    /// Fecha de referencia del bloque operativo. No representa
    /// necesariamente la fecha calendario del primer servicio.
    /// </summary>
    public DateOnly FechaOperativa { get; set; }

    /// <summary>Empresa a la que pertenece la jornada.</summary>
    public Empresa? Empresa { get; set; }

    /// <summary>Servicios que forman parte de esta jornada.</summary>
    public ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
}
