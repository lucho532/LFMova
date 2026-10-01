using TransportApp.Application.DTOs.Empleados;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define los casos de uso de gestión directa de empleados por parte de un
/// coordinador: consulta, actualización de datos actuales y
/// activación/desactivación. No incluye el cambio de empresa de un empleado:
/// esa operación se resuelve exclusivamente mediante la importación de Excel
/// (ver <c>spec.md</c> §39). No decide autorización: eso se verifica en la
/// capa de Api antes de invocar estos métodos.
/// </summary>
public interface IEmpleadoServicio
{
    /// <summary>Obtiene los empleados de la empresa indicada.</summary>
    Task<List<EmpleadoDto>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>
    /// Obtiene un empleado por su identificador, validando que pertenezca a
    /// la empresa indicada. Devuelve <c>null</c> si no existe o no pertenece
    /// a esa empresa.
    /// </summary>
    Task<EmpleadoDto?> ObtenerPorIdAsync(int empresaId, int empleadoId);

    /// <summary>Actualiza los datos actuales de un empleado de la empresa indicada.</summary>
    Task ActualizarAsync(int empresaId, int empleadoId, ActualizarEmpleadoDto datos);
}
