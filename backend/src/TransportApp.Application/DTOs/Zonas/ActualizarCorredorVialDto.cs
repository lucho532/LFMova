namespace TransportApp.Application.DTOs.Zonas;

/// <summary>Datos de entrada para actualizar el nombre de un corredor vial.</summary>
public class ActualizarCorredorVialDto
{
    /// <summary>Nombre del corredor vial.</summary>
    public string Nombre { get; set; } = string.Empty;
}
