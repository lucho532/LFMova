namespace TransportApp.Application.DTOs.Zonas;

/// <summary>Zona con la que se une otra zona de la misma empresa.</summary>
public class UnirZonaDto
{
    /// <summary>Zona que recibe todos los barrios y que se conserva.</summary>
    public int ZonaDestinoId { get; set; }
}
