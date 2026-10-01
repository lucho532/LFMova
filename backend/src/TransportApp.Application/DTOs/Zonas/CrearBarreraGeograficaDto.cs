namespace TransportApp.Application.DTOs.Zonas;

/// <summary>Datos de entrada para declarar una barrera geográfica entre dos barrios.</summary>
public class CrearBarreraGeograficaDto
{
    /// <summary>Uno de los dos barrios sin conexión aprovechable entre sí.</summary>
    public string BarrioA { get; set; } = string.Empty;

    /// <summary>El otro barrio sin conexión aprovechable con <see cref="BarrioA"/>.</summary>
    public string BarrioB { get; set; } = string.Empty;

    /// <summary>Motivo de la barrera, para documentarla (opcional).</summary>
    public string? Motivo { get; set; }
}
