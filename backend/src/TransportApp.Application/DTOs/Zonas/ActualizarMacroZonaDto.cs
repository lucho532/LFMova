namespace TransportApp.Application.DTOs.Zonas;

/// <summary>Datos de entrada para actualizar el nombre de una macrozona.</summary>
public class ActualizarMacroZonaDto
{
    /// <summary>Nombre de la macrozona.</summary>
    public string Nombre { get; set; } = string.Empty;
}
