namespace LFMova.Application.DTOs.Zonas;

/// <summary>Orden en que el coordinador quiere ver las zonas de su empresa.</summary>
public class ReordenarZonasDto
{
    /// <summary>Identificadores de las zonas, de la primera a la última.</summary>
    public List<int> ZonaIds { get; set; } = new();
}
