namespace LFMova.Application.DTOs.Servicios;

/// <summary>Datos necesarios para cambiar la sede de una ruta que todavía no ha salido.</summary>
public class CambiarSedeServicioDto
{
    /// <summary>Identificador de la sede nueva (debe ser una sede activa de la misma empresa).</summary>
    public int SedeId { get; set; }
}
