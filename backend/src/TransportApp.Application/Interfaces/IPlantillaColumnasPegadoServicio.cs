using TransportApp.Application.DTOs.Importaciones;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define los casos de uso sobre la plantilla de orden de columnas que una
/// empresa usa para crear rutas pegando filas de un Excel externo. No decide
/// autorización: eso se verifica en la capa de Api.
/// </summary>
public interface IPlantillaColumnasPegadoServicio
{
    /// <summary>Obtiene la plantilla de la empresa, o <c>null</c> si todavía no definió ninguna.</summary>
    Task<PlantillaColumnasPegadoDto?> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>
    /// Guarda la plantilla de la empresa: si ya existía una, se reemplaza
    /// (queda como máximo una por empresa). Valida
    /// <see cref="GuardarPlantillaColumnasPegadoDto.ColumnasEnOrden"/> según
    /// las reglas documentadas ahí (cinco campos obligatorios una vez cada
    /// uno, APELLIDOS opcional como máximo una vez, IGNORAR sin límite).
    /// </summary>
    Task<PlantillaColumnasPegadoDto> GuardarAsync(int empresaId, GuardarPlantillaColumnasPegadoDto datos);
}
