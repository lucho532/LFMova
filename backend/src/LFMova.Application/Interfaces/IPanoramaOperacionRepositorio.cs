using LFMova.Application.DTOs.Estadisticas;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Consulta de solo lectura que agrega la operación de una empresa en un periodo.
/// No decide reglas de autorización ni valida el rango.
/// </summary>
public interface IPanoramaOperacionRepositorio
{
    /// <summary>Indicadores y serie diaria de la empresa entre <paramref name="desde"/> y <paramref name="hasta"/> (ambos incluidos).</summary>
    Task<PanoramaOperacionDto> ObtenerAsync(int empresaId, DateOnly desde, DateOnly hasta);
}
