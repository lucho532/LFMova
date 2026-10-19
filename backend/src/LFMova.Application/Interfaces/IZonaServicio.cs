using LFMova.Application.DTOs.Zonas;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define los casos de uso de administración de zonas geográficas de
/// recogida de una empresa. No decide autorización: eso se verifica en la
/// capa de Api antes de invocar estos métodos.
/// </summary>
public interface IZonaServicio
{
    /// <summary>Crea una nueva zona para la empresa indicada.</summary>
    Task<ZonaDto> CrearAsync(int empresaId, CrearZonaDto datos);

    /// <summary>
    /// Obtiene una zona por su identificador, validando que pertenezca a la
    /// empresa indicada. Devuelve <c>null</c> si no existe o no pertenece a
    /// esa empresa.
    /// </summary>
    Task<ZonaDto?> ObtenerPorIdAsync(int empresaId, int zonaId);

    /// <summary>Obtiene todas las zonas de la empresa indicada.</summary>
    Task<List<ZonaDto>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Actualiza el nombre y los barrios de una zona de la empresa indicada.</summary>
    Task ActualizarAsync(int empresaId, int zonaId, ActualizarZonaDto datos);

    /// <summary>
    /// Agrega un barrio como alias adicional de una zona ya existente (por ejemplo, cuando un barrio del
    /// Excel no coincidía con ninguna zona y el coordinador decide que es el mismo lugar que otra zona
    /// ya tiene con otro nombre). Si el barrio ya está en la zona, no hace nada.
    /// </summary>
    Task<ZonaDto> AgregarBarrioAsync(int empresaId, int zonaId, string barrio);

    /// <summary>Activa una zona de la empresa indicada.</summary>
    Task ActivarAsync(int empresaId, int zonaId);

    /// <summary>Desactiva una zona de la empresa indicada.</summary>
    Task DesactivarAsync(int empresaId, int zonaId);
}
