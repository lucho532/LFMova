namespace TransportApp.Domain.Entities;

/// <summary>
/// Representa un vehículo perteneciente exclusivamente a un <c>Conductor</c>.
/// Un vehículo no puede ser utilizado temporalmente por otro conductor.
/// No decide si puede formar una <c>UnidadOperativa</c>; esa validación de
/// consistencia corresponde a la capa de reglas de dominio/aplicación.
/// </summary>
public class Vehiculo
{
    /// <summary>Identificador único del vehículo.</summary>
    public int VehiculoId { get; set; }

    /// <summary>Identificador del conductor propietario exclusivo del vehículo.</summary>
    public int ConductorId { get; set; }

    /// <summary>Placa del vehículo. Debe ser única.</summary>
    public string Placa { get; set; } = string.Empty;

    /// <summary>Marca del vehículo.</summary>
    public string Marca { get; set; } = string.Empty;

    /// <summary>Modelo del vehículo.</summary>
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Capacidad de pasajeros del vehículo.</summary>
    public int Capacidad { get; set; }

    /// <summary>
    /// Indica si el vehículo está activo. Un vehículo histórico se desactiva,
    /// no se elimina físicamente.
    /// </summary>
    /// <summary>
    /// Fecha hasta la que está vigente el SOAT (seguro obligatorio) del
    /// vehículo. Es <c>null</c> solo en vehículos registrados antes de
    /// exigirse este dato.
    /// </summary>
    public DateOnly? VigenciaSoat { get; set; }

    /// <summary>
    /// Fecha hasta la que está vigente la revisión técnico-mecánica del
    /// vehículo. Es <c>null</c> solo en vehículos registrados antes de
    /// exigirse este dato.
    /// </summary>
    public DateOnly? VigenciaTecnomecanica { get; set; }

    public bool Activo { get; set; }

    /// <summary>Conductor propietario exclusivo del vehículo.</summary>
    public Conductor? Conductor { get; set; }

    /// <summary>Unidad operativa formada con este vehículo, si existe.</summary>
    public UnidadOperativa? UnidadOperativa { get; set; }
}
