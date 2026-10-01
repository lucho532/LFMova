namespace LFMova.Domain.Entities;

/// <summary>
/// Representa una ubicación física de una empresa.
/// Su responsabilidad es identificar y describir el lugar (nombre, dirección,
/// ciudad, barrio y coordenadas) al que se dirigen o desde el que parten los
/// servicios de transporte de la empresa.
/// No decide reglas de planificación, geocodificación ni de programación de
/// transporte; tampoco valida por sí misma la pertenencia a una empresa.
/// </summary>
public class Sede
{
    /// <summary>Identificador único de la sede.</summary>
    public int SedeId { get; set; }

    /// <summary>Identificador de la empresa a la que pertenece esta sede.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Nombre de la sede.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Dirección de la sede.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Ciudad donde se ubica la sede. Es un dato descriptivo.</summary>
    public string Ciudad { get; set; } = string.Empty;

    /// <summary>Barrio donde se ubica la sede. Es un dato descriptivo.</summary>
    public string Barrio { get; set; } = string.Empty;

    /// <summary>
    /// Latitud de la sede. Puede ser <c>null</c> cuando la coordenada
    /// todavía no está disponible.
    /// </summary>
    public double? Latitud { get; set; }

    /// <summary>
    /// Longitud de la sede. Puede ser <c>null</c> cuando la coordenada
    /// todavía no está disponible.
    /// </summary>
    public double? Longitud { get; set; }

    /// <summary>
    /// Indica si la sede está activa. Una sede inactiva conserva su
    /// información histórica y no se elimina físicamente, pero no debe
    /// ofrecerse para nueva programación.
    /// </summary>
    public bool Activa { get; set; }

    /// <summary>Empresa a la que pertenece esta sede.</summary>
    public Empresa? Empresa { get; set; }

    /// <summary>Programaciones de transporte asociadas a esta sede.</summary>
    public ICollection<ProgramacionTransporte> ProgramacionesTransporte { get; set; } = new List<ProgramacionTransporte>();

    /// <summary>Servicios asociados a esta sede.</summary>
    public ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
}
