using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Application.DTOs.Facturacion;
using LFMova.Application.Interfaces;

namespace LFMova.Api.Controllers;

/// <summary>
/// Facturación de la plataforma, solo para el administrador: cuántos
/// conductores finalizaron rutas en cada empresa por mes, el cierre mensual y
/// su soporte en Excel. No contiene lógica de negocio: delega en
/// <see cref="IFacturacionServicio"/>. Es de solo lectura sobre la operación.
/// </summary>
[ApiController]
[Route("api/facturacion")]
[Authorize(Policy = "RequiereAdministradorPlataforma")]
public class FacturacionController : ControllerBase
{
    private const string TipoExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IFacturacionServicio _facturacionServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public FacturacionController(IFacturacionServicio facturacionServicio)
    {
        _facturacionServicio = facturacionServicio;
    }

    /// <summary>Resumen de uso de todas las empresas en el mes indicado.</summary>
    [HttpGet]
    public Task<ActionResult<List<ResumenFacturacionEmpresaDto>>> ObtenerResumenAsync([FromQuery] int anio, [FromQuery] int mes)
        => EjecutarAsync(() => _facturacionServicio.ObtenerResumenAsync(anio, mes));

    /// <summary>Detalle de una empresa en el mes: sus totales y los conductores que finalizaron rutas.</summary>
    [HttpGet("empresas/{empresaId:int}")]
    public Task<ActionResult<DetalleFacturacionDto>> ObtenerDetalleAsync(int empresaId, [FromQuery] int anio, [FromQuery] int mes)
        => EjecutarAsync(() => _facturacionServicio.ObtenerDetalleAsync(empresaId, anio, mes));

    /// <summary>Cierra el mes de una empresa, dejando fijos sus totales. Solo se puede cerrar un mes ya terminado.</summary>
    [HttpPost("empresas/{empresaId:int}/cierres")]
    public Task<ActionResult<DetalleFacturacionDto>> CerrarMesAsync(int empresaId, [FromQuery] int anio, [FromQuery] int mes)
        => EjecutarAsync(() => _facturacionServicio.CerrarMesAsync(empresaId, anio, mes));

    /// <summary>Descarga el Excel de soporte del mes de una empresa.</summary>
    [HttpGet("empresas/{empresaId:int}/excel")]
    public async Task<IActionResult> DescargarAsync(int empresaId, [FromQuery] int anio, [FromQuery] int mes)
    {
        try
        {
            var archivo = await _facturacionServicio.DescargarAsync(empresaId, anio, mes);
            return File(archivo.Contenido, TipoExcel, archivo.NombreArchivo);
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    private async Task<ActionResult<T>> EjecutarAsync<T>(Func<Task<T>> accion)
    {
        try
        {
            return Ok(await accion());
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }
}
