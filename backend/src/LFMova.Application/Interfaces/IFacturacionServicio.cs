using LFMova.Application.DTOs.Facturacion;
using LFMova.Application.DTOs.Soportes;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Casos de uso de facturación del administrador de plataforma: cuánto usó
/// la aplicación cada empresa en un mes (por conductores que finalizaron
/// rutas), el cierre mensual y su soporte en Excel. Es de solo lectura sobre
/// la operación: no modifica rutas, conductores ni empresas.
/// </summary>
public interface IFacturacionServicio
{
    /// <summary>Resumen de todas las empresas para el mes. Falla si el mes no es válido.</summary>
    Task<List<ResumenFacturacionEmpresaDto>> ObtenerResumenAsync(int anio, int mes);

    /// <summary>Detalle de una empresa en el mes, con sus conductores. Falla si la empresa no existe.</summary>
    Task<DetalleFacturacionDto> ObtenerDetalleAsync(int empresaId, int anio, int mes);

    /// <summary>
    /// Cierra el mes de una empresa dejando fijos sus totales. Falla si el
    /// mes todavía no ha terminado en Colombia o si ya estaba cerrado.
    /// </summary>
    Task<DetalleFacturacionDto> CerrarMesAsync(int empresaId, int anio, int mes);

    /// <summary>Genera el Excel de soporte del mes de una empresa (cerrado o no).</summary>
    Task<ArchivoSoporte> DescargarAsync(int empresaId, int anio, int mes);
}
