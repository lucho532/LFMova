using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportApp.Api.Configuration;
using TransportApp.Application.DTOs.Zonas;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;
using TransportApp.Infrastructure.Autenticacion;

namespace TransportApp.Api.Controllers;

/// <summary>
/// Expone la administración de corredores viales (agrupaciones de zonas que
/// comparten vehículo porque están sobre el mismo camino real) de una
/// empresa. No contiene lógica de negocio: delega en
/// <see cref="ICorredorVialServicio"/> y verifica el contexto de empresa
/// antes de invocarlo.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/corredores-viales")]
[Authorize]
public class CorredoresVialesController : ControllerBase
{
    private readonly ICorredorVialServicio _corredorVialServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public CorredoresVialesController(ICorredorVialServicio corredorVialServicio)
    {
        _corredorVialServicio = corredorVialServicio;
    }

    /// <summary>Crea un corredor vial para la empresa. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost]
    public async Task<ActionResult<CorredorVialDto>> CrearAsync(int empresaId, CrearCorredorVialDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        var corredor = await _corredorVialServicio.CrearAsync(empresaId, datos);
        return Ok(corredor);
    }

    /// <summary>Consulta los corredores viales de la empresa. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpGet]
    public async Task<ActionResult<List<CorredorVialDto>>> ObtenerPorEmpresaAsync(int empresaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _corredorVialServicio.ObtenerPorEmpresaAsync(empresaId));
    }

    /// <summary>Activa un corredor vial. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost("{corredorVialId:int}/activar")]
    public async Task<IActionResult> ActivarAsync(int empresaId, int corredorVialId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _corredorVialServicio.ActivarAsync(empresaId, corredorVialId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Desactiva un corredor vial. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost("{corredorVialId:int}/desactivar")]
    public async Task<IActionResult> DesactivarAsync(int empresaId, int corredorVialId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _corredorVialServicio.DesactivarAsync(empresaId, corredorVialId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }
}
