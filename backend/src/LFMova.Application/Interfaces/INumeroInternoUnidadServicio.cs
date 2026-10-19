using LFMova.Application.DTOs.UnidadesOperativas;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Casos de uso del número interno que una empresa asigna a las unidades operativas de sus conductores.
/// No decide autorización: eso se verifica en la capa de Api.
/// </summary>
public interface INumeroInternoUnidadServicio
{
    /// <summary>Números internos que la empresa ha asignado a sus unidades.</summary>
    Task<List<NumeroInternoUnidadDto>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>
    /// Asigna, cambia o (con <paramref name="numero"/> vacío) quita el número interno de la unidad para la
    /// empresa. La unidad debe ser de un conductor con vinculación activa a la empresa y el número no puede
    /// repetirse en otra unidad de la misma empresa.
    /// </summary>
    Task AsignarAsync(int empresaId, int unidadOperativaId, string? numero);
}
