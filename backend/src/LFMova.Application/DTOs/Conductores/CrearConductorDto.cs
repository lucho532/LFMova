using LFMova.Application.DTOs.Vehiculos;

namespace LFMova.Application.DTOs.Conductores;

/// <summary>
/// Datos necesarios para asignar el rol de conductor a una persona que ya
/// se registró. El nombre y el teléfono del conductor se toman de su propia
/// cuenta; nunca se crea una cuenta nueva desde aquí. Al crear al conductor
/// se registra también su vehículo y se forma su unidad operativa
/// (conductor + vehículo), de modo que quede listo para asignarse a
/// servicios.
/// </summary>
public class CrearConductorDto
{
    /// <summary>Cédula de la persona, que debe tener ya una cuenta registrada.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Vehículo del conductor, con el que se forma su unidad operativa.</summary>
    public CrearVehiculoDto Vehiculo { get; set; } = new();
}
