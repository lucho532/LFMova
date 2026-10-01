using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportApp.Api.Configuration;
using TransportApp.Application.DTOs.Zonas;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;
using TransportApp.Infrastructure.Autenticacion;

namespace TransportApp.Api.Controllers;

/// <summary>
/// Expone la administración de macrozonas (agrupaciones organizativas de
/// zonas, por ejemplo por comuna) de una empresa. No contiene lógica de
/// negocio: delega en <see cref="IMacroZonaServicio"/> y verifica el
/// contexto de empresa antes de invocarlo.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/macro-zonas")]
[Authorize]
public class MacroZonasController : ControllerBase
{
    private readonly IMacroZonaServicio _macroZonaServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public MacroZonasController(IMacroZonaServicio macroZonaServicio)
    {
        _macroZonaServicio = macroZonaServicio;
    }

    /// <summary>Crea una macrozona para la empresa. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost]
    public async Task<ActionResult<MacroZonaDto>> CrearAsync(int empresaId, CrearMacroZonaDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        var macroZona = await _macroZonaServicio.CrearAsync(empresaId, datos);
        return Ok(macroZona);
    }

    /// <summary>Consulta las macrozonas de la empresa. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpGet]
    public async Task<ActionResult<List<MacroZonaDto>>> ObtenerPorEmpresaAsync(int empresaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _macroZonaServicio.ObtenerPorEmpresaAsync(empresaId));
    }

    /// <summary>Activa una macrozona. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost("{macroZonaId:int}/activar")]
    public async Task<IActionResult> ActivarAsync(int empresaId, int macroZonaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _macroZonaServicio.ActivarAsync(empresaId, macroZonaId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Desactiva una macrozona. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost("{macroZonaId:int}/desactivar")]
    public async Task<IActionResult> DesactivarAsync(int empresaId, int macroZonaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _macroZonaServicio.DesactivarAsync(empresaId, macroZonaId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }
}
