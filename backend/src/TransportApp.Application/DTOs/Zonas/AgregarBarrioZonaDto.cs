namespace TransportApp.Application.DTOs.Zonas;

/// <summary>Barrio que se agrega a una zona ya existente, como alias adicional.</summary>
public class AgregarBarrioZonaDto
{
    /// <summary>Nombre del barrio a agregar.</summary>
    public string Barrio { get; set; } = string.Empty;
}
