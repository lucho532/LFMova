namespace TransportApp.Domain.Entities;

/// <summary>
/// Representa la combinación operacional entre un <c>Conductor</c> y un
/// <c>Vehiculo</c>. Es la entidad que se asigna individualmente a cada
/// <c>Servicio</c> (no a la <c>Jornada</c> como un todo).
/// Un vehículo puede tener como máximo una unidad operativa. No valida por sí
/// misma que <c>ConductorId</c> coincida con el conductor propietario del
/// vehículo; esa regla corresponde a la capa de reglas de dominio.
/// </summary>
public class UnidadOperativa
{
    /// <summary>Identificador único de la unidad operativa.</summary>
    public int UnidadOperativaId { get; set; }

    /// <summary>Identificador del conductor de la unidad.</summary>
    public int ConductorId { get; set; }

    /// <summary>Identificador del vehículo de la unidad.</summary>
    public int VehiculoId { get; set; }

    /// <summary>
    /// Indica si la unidad operativa está activa. Al dejar de utilizarse un
    /// vehículo, la unidad se desactiva y se crea una nueva para el vehículo
    /// que lo reemplace; no se reutiliza ni se modifica esta unidad.
    /// </summary>
    public bool Activa { get; set; }

    /// <summary>Conductor de la unidad.</summary>
    public Conductor? Conductor { get; set; }

    /// <summary>Vehículo de la unidad.</summary>
    public Vehiculo? Vehiculo { get; set; }

    /// <summary>Servicios asignados a esta unidad operativa.</summary>
    public ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
}
