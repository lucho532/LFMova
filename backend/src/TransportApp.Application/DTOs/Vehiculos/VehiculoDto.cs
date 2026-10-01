namespace TransportApp.Application.DTOs.Vehiculos;

/// <summary>Representación pública de un vehículo.</summary>
public class VehiculoDto
{
    /// <summary>Identificador único del vehículo.</summary>
    public int VehiculoId { get; set; }

    /// <summary>Identificador del conductor propietario exclusivo del vehículo.</summary>
    public int ConductorId { get; set; }

    /// <summary>Placa del vehículo.</summary>
    public string Placa { get; set; } = string.Empty;

    /// <summary>Marca del vehículo.</summary>
    public string Marca { get; set; } = string.Empty;

    /// <summary>Modelo del vehículo.</summary>
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Capacidad de pasajeros del vehículo.</summary>
    public int Capacidad { get; set; }

    /// <summary>Fecha hasta la que está vigente el SOAT, o <c>null</c> si no se registró.</summary>
    public DateOnly? VigenciaSoat { get; set; }

    /// <summary>Fecha hasta la que está vigente la revisión técnico-mecánica, o <c>null</c> si no se registró.</summary>
    public DateOnly? VigenciaTecnomecanica { get; set; }

    /// <summary>Indica si el vehículo está activo.</summary>
    public bool Activo { get; set; }
}
