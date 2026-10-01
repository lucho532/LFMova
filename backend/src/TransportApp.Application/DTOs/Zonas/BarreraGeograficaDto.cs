namespace TransportApp.Application.DTOs.Zonas;

/// <summary>Representación pública de una barrera geográfica.</summary>
public class BarreraGeograficaDto
{
    /// <summary>Identificador único de la barrera.</summary>
    public int BarreraGeograficaId { get; set; }

    /// <summary>Identificador de la empresa a la que pertenece la barrera.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Uno de los dos barrios sin conexión aprovechable entre sí.</summary>
    public string BarrioA { get; set; } = string.Empty;

    /// <summary>El otro barrio sin conexión aprovechable con <see cref="BarrioA"/>.</summary>
    public string BarrioB { get; set; } = string.Empty;

    /// <summary>Motivo de la barrera, si se documentó.</summary>
    public string? Motivo { get; set; }
}
