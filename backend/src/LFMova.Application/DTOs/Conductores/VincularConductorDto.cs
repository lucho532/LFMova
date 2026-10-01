namespace LFMova.Application.DTOs.Conductores;

/// <summary>
/// Datos necesarios para vincular con una empresa un conductor que ya está
/// registrado en la plataforma.
/// </summary>
public class VincularConductorDto
{
    /// <summary>Cédula del conductor a vincular. Debe corresponder a un conductor ya registrado.</summary>
    public string Cedula { get; set; } = string.Empty;
}
