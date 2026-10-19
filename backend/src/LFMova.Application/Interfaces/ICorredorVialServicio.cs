using LFMova.Application.DTOs.Zonas;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define los casos de uso de administración de corredores viales de una
/// empresa. No decide autorización: eso se verifica en la capa de Api antes
/// de invocar estos métodos.
/// </summary>
public interface ICorredorVialServicio
{
    /// <summary>Crea un nuevo corredor vial para la empresa indicada.</summary>
    Task<CorredorVialDto> CrearAsync(int empresaId, CrearCorredorVialDto datos);

    /// <summary>Obtiene todos los corredores viales de la empresa indicada.</summary>
    Task<List<CorredorVialDto>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Activa un corredor vial de la empresa indicada.</summary>
    Task ActivarAsync(int empresaId, int corredorVialId);

    /// <summary>Desactiva un corredor vial de la empresa indicada.</summary>
    Task DesactivarAsync(int empresaId, int corredorVialId);
}
