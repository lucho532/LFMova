using LFMova.Application.DTOs.Soportes;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Convierte el soporte de rutas de un conductor en un archivo Excel
/// (.xlsx). Solo da formato al contenido que recibe: no consulta datos ni
/// decide qué rutas incluye.
/// </summary>
public interface IGeneradorSoporteRutas
{
    /// <summary>Genera el archivo Excel del soporte: una hoja con un bloque por cada ruta.</summary>
    byte[] Generar(SoporteRutasConductor soporte);
}
