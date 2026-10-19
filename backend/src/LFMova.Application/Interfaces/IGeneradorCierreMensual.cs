using LFMova.Application.DTOs.Facturacion;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Genera el archivo Excel de soporte de la facturación de una empresa en un
/// mes. Solo da formato a los datos que recibe: no los consulta ni decide
/// qué se cobra.
/// </summary>
public interface IGeneradorCierreMensual
{
    /// <summary>Devuelve el contenido del archivo .xlsx con los totales y el detalle por conductor.</summary>
    byte[] Generar(DetalleFacturacionDto detalle);
}
