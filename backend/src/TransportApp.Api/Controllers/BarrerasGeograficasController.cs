using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportApp.Api.Configuration;
using TransportApp.Application.DTOs.Zonas;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;
using TransportApp.Infrastructure.Autenticacion;

namespace TransportApp.Api.Controllers;

/// <summary>
/// Expone la administración de barreras geográficas declaradas entre
/// barrios de una empresa (pares de barrios que nunca deben combinarse en
/// una misma zona, por ejemplo por estar separados por un accidente
/// topográfico sin vía de conexión útil). No contiene lógica de negocio:
/// delega en <see cref="IBarreraGeograficaServicio"/> y verifica el
/// contexto de empresa antes de invocarlo.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/barreras-geograficas")]
[Authorize]
public class BarrerasGeograficasController : ControllerBase
{
    private readonly IBarreraGeograficaServicio _barreraGeograficaServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public BarrerasGeograficasController(IBarreraGeograficaServicio barreraGeograficaServicio)
    {
        _barreraGeograficaServicio = barreraGeograficaServicio;
    }

    /// <summary>Declara una barrera geográfica entre dos barrios de la empresa. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost]
    public async Task<ActionResult<BarreraGeograficaDto>> CrearAsync(int empresaId, CrearBarreraGeograficaDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        var barrera = await _barreraGeograficaServicio.CrearAsync(empresaId, datos);
        return Ok(barrera);
    }

    /// <summary>Consulta las barreras geográficas de la empresa. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpGet]
    public async Task<ActionResult<List<BarreraGeograficaDto>>> ObtenerPorEmpresaAsync(int empresaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _barreraGeograficaServicio.ObtenerPorEmpresaAsync(empresaId));
    }

    /// <summary>Elimina una barrera geográfica. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpDelete("{barreraGeograficaId:int}")]
    public async Task<IActionResult> EliminarAsync(int empresaId, int barreraGeograficaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _barreraGeograficaServicio.EliminarAsync(empresaId, barreraGeograficaId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }
}
