namespace LFMova.Application.DTOs.Zonas;

/// <summary>Barrio que se pasa de una zona a otra zona ya existente de la misma empresa.</summary>
public class MoverBarrioZonaDto
{
    /// <summary>Nombre del barrio a mover, tal como está en la zona de origen.</summary>
    public string Barrio { get; set; } = string.Empty;

    /// <summary>Zona que recibe el barrio.</summary>
    public int ZonaDestinoId { get; set; }
}
