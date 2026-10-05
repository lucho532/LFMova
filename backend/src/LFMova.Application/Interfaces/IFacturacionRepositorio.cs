using LFMova.Application.DTOs.Facturacion;
using LFMova.Domain.Entities;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Persistencia de la facturación: el registro de rutas finalizadas por
/// conductor y los cierres mensuales. Solo guarda y consulta; qué se cobra y
/// cuándo se puede cerrar lo decide <see cref="IFacturacionServicio"/>.
/// </summary>
public interface IFacturacionRepositorio
{
    /// <summary>Agrega el registro de uso de una ruta; se guarda con el siguiente guardado del contexto.</summary>
    Task AgregarUsoAsync(UsoConductor uso);

    /// <summary>Totales de uso de cada empresa entre dos fechas (todas las empresas, también las que no tuvieron rutas).</summary>
    Task<List<ResumenFacturacionEmpresaDto>> ObtenerResumenAsync(DateOnly desde, DateOnly hasta);

    /// <summary>Conductores que finalizaron rutas para la empresa entre dos fechas, ordenados por nombre.</summary>
    Task<List<ConductorFacturableDto>> ObtenerConductoresAsync(int empresaId, DateOnly desde, DateOnly hasta);

    /// <summary>Cierres existentes de un mes, de todas las empresas.</summary>
    Task<List<CierreMensual>> ObtenerCierresAsync(int anio, int mes);

    /// <summary>Agrega un cierre mensual.</summary>
    Task AgregarCierreAsync(CierreMensual cierre);

    /// <summary>Persiste los cambios pendientes.</summary>
    Task GuardarCambiosAsync();
}
