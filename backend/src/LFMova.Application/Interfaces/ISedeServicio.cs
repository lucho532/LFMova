using LFMova.Application.DTOs.Sedes;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define los casos de uso de administración de sedes de una empresa. No
/// decide autorización: eso se verifica en la capa de Api antes de invocar
/// estos métodos.
/// </summary>
public interface ISedeServicio
{
    /// <summary>Crea una nueva sede para la empresa indicada.</summary>
    Task<SedeDto> CrearAsync(int empresaId, CrearSedeDto datos);

    /// <summary>
    /// Obtiene una sede por su identificador, validando que pertenezca a la
    /// empresa indicada. Devuelve <c>null</c> si no existe o no pertenece a
    /// esa empresa.
    /// </summary>
    Task<SedeDto?> ObtenerPorIdAsync(int empresaId, int sedeId);

    /// <summary>Obtiene todas las sedes de la empresa indicada.</summary>
    Task<List<SedeDto>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Actualiza los datos actuales de una sede de la empresa indicada.</summary>
    Task ActualizarAsync(int empresaId, int sedeId, ActualizarSedeDto datos);

    /// <summary>Activa una sede de la empresa indicada.</summary>
    Task ActivarAsync(int empresaId, int sedeId);

    /// <summary>Desactiva una sede de la empresa indicada.</summary>
    Task DesactivarAsync(int empresaId, int sedeId);
}
