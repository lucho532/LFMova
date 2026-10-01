using TransportApp.Application.DTOs.Zonas;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define los casos de uso de administración de macrozonas de una empresa.
/// No decide autorización: eso se verifica en la capa de Api antes de
/// invocar estos métodos.
/// </summary>
public interface IMacroZonaServicio
{
    /// <summary>Crea una nueva macrozona para la empresa indicada.</summary>
    Task<MacroZonaDto> CrearAsync(int empresaId, CrearMacroZonaDto datos);

    /// <summary>Obtiene todas las macrozonas de la empresa indicada.</summary>
    Task<List<MacroZonaDto>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Actualiza el nombre de una macrozona de la empresa indicada.</summary>
    Task ActualizarAsync(int empresaId, int macroZonaId, ActualizarMacroZonaDto datos);

    /// <summary>Activa una macrozona de la empresa indicada.</summary>
    Task ActivarAsync(int empresaId, int macroZonaId);

    /// <summary>Desactiva una macrozona de la empresa indicada.</summary>
    Task DesactivarAsync(int empresaId, int macroZonaId);
}
