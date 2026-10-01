using TransportApp.Application.DTOs.Programaciones;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define los casos de uso de administración de programaciones de transporte
/// de una empresa. No decide autorización: eso se verifica en la capa de Api
/// antes de invocar estos métodos.
/// </summary>
public interface IProgramacionTransporteServicio
{
    /// <summary>
    /// Crea una nueva programación para la empresa indicada. Valida que el
    /// empleado y la sede pertenezcan a esa misma empresa.
    /// </summary>
    Task<ProgramacionDto> CrearAsync(int empresaId, CrearProgramacionDto datos);

    /// <summary>
    /// Obtiene una programación por su identificador, validando que
    /// pertenezca a la empresa indicada. Devuelve <c>null</c> si no existe o
    /// no pertenece a esa empresa.
    /// </summary>
    Task<ProgramacionDto?> ObtenerPorIdAsync(int empresaId, int programacionTransporteId);

    /// <summary>Obtiene todas las programaciones de la empresa indicada.</summary>
    Task<List<ProgramacionDto>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>
    /// Actualiza los datos de una programación de la empresa indicada.
    /// Valida que la sede pertenezca a esa misma empresa.
    /// </summary>
    Task ActualizarAsync(int empresaId, int programacionTransporteId, ActualizarProgramacionDto datos);
}
