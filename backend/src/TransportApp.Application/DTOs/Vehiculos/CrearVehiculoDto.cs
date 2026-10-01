namespace TransportApp.Application.DTOs.Vehiculos;

/// <summary>Datos necesarios para registrar un nuevo vehículo.</summary>
public class CrearVehiculoDto
{
    /// <summary>Placa del vehículo. Debe ser única.</summary>
    public string Placa { get; set; } = string.Empty;

    /// <summary>Marca del vehículo.</summary>
    public string Marca { get; set; } = string.Empty;

    /// <summary>Modelo del vehículo.</summary>
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Capacidad de pasajeros del vehículo.</summary>
    public int Capacidad { get; set; }

    /// <summary>Fecha hasta la que está vigente el SOAT del vehículo.</summary>
    public DateOnly VigenciaSoat { get; set; }

    /// <summary>Fecha hasta la que está vigente la revisión técnico-mecánica del vehículo.</summary>
    public DateOnly VigenciaTecnomecanica { get; set; }
}
