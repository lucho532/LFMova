using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.Soportes;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Api.Controllers;

/// <summary>
/// Entrega el informe de asistencia de todas las rutas de la empresa en una jornada o rango de días como un
/// único Excel. No contiene lógica de negocio: delega en <see cref="IInformeAsistenciaPeriodoServicio"/> y
/// verifica que quien lo pide sea coordinador de la empresa.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/informe-asistencia")]
[Authorize]
public class InformeAsistenciaEmpresaController : ControllerBase
{
    private readonly IInformeAsistenciaPeriodoServicio _informeServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public InformeAsistenciaEmpresaController(IInformeAsistenciaPeriodoServicio informeServicio)
    {
        _informeServicio = informeServicio;
    }

    /// <summary>
    /// Devuelve los mismos datos que lleva el Excel, para mostrarlos en una vista previa antes de descargar.
    /// Solo un coordinador de esa empresa puede consultarlos.
    /// </summary>
    [HttpGet("vista-previa")]
    public async Task<ActionResult<InformeAsistenciaPeriodo>> VistaPreviaAsync(int empresaId, [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] FiltroAsistenciaRuta filtro = FiltroAsistenciaRuta.NO_ASISTIERON)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _informeServicio.ObtenerAsync(empresaId, desde, hasta, filtro));
        }
        catch (ArgumentException excepcion)
        {
            return BadRequest(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Descarga el Excel con los pasajeros que cumplen el filtro (<c>TODOS</c>, <c>ASISTIERON</c> o
    /// <c>NO_ASISTIERON</c>) en las rutas de la empresa entre <paramref name="desde"/> y
    /// <paramref name="hasta"/>, ambas incluidas. Solo un coordinador de esa empresa puede hacerlo.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DescargarAsync(int empresaId, [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] FiltroAsistenciaRuta filtro = FiltroAsistenciaRuta.NO_ASISTIERON)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            var archivo = await _informeServicio.GenerarAsync(empresaId, desde, hasta, filtro);
            return File(archivo.Contenido, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", archivo.NombreArchivo);
        }
        catch (ArgumentException excepcion)
        {
            return BadRequest(new { mensaje = excepcion.Message });
        }
    }
}
