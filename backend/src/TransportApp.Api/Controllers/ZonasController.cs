using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportApp.Api.Configuration;
using TransportApp.Application.DTOs.Zonas;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;
using TransportApp.Domain.Exceptions;
using TransportApp.Infrastructure.Autenticacion;

namespace TransportApp.Api.Controllers;

/// <summary>
/// Expone la administración de zonas geográficas de recogida de una empresa.
/// No contiene lógica de negocio: delega en <see cref="IZonaServicio"/> y
/// verifica el contexto de empresa antes de invocarlo.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/zonas")]
[Authorize]
public class ZonasController : ControllerBase
{
    private readonly IZonaServicio _zonaServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public ZonasController(IZonaServicio zonaServicio)
    {
        _zonaServicio = zonaServicio;
    }

    /// <summary>Crea una zona para la empresa. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost]
    public async Task<ActionResult<ZonaDto>> CrearAsync(int empresaId, CrearZonaDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            var zona = await _zonaServicio.CrearAsync(empresaId, datos);
            return CreatedAtAction(nameof(ObtenerPorIdAsync), new { empresaId, zonaId = zona.ZonaId }, zona);
        }
        catch (BarreraGeograficaConflictoException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Consulta las zonas de la empresa. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ZonaDto>>> ObtenerPorEmpresaAsync(int empresaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _zonaServicio.ObtenerPorEmpresaAsync(empresaId));
    }

    /// <summary>Consulta una zona concreta de la empresa.</summary>
    [HttpGet("{zonaId:int}")]
    public async Task<ActionResult<ZonaDto>> ObtenerPorIdAsync(int empresaId, int zonaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        var zona = await _zonaServicio.ObtenerPorIdAsync(empresaId, zonaId);
        return zona is null ? NotFound() : Ok(zona);
    }

    /// <summary>Actualiza el nombre y los barrios de una zona. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPut("{zonaId:int}")]
    public async Task<IActionResult> ActualizarAsync(int empresaId, int zonaId, ActualizarZonaDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _zonaServicio.ActualizarAsync(empresaId, zonaId, datos);
            return NoContent();
        }
        catch (BarreraGeograficaConflictoException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Agrega un barrio como alias adicional de una zona ya existente. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost("{zonaId:int}/barrios")]
    public async Task<ActionResult<ZonaDto>> AgregarBarrioAsync(int empresaId, int zonaId, AgregarBarrioZonaDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _zonaServicio.AgregarBarrioAsync(empresaId, zonaId, datos.Barrio));
        }
        catch (BarreraGeograficaConflictoException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Activa una zona. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost("{zonaId:int}/activar")]
    public async Task<IActionResult> ActivarAsync(int empresaId, int zonaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _zonaServicio.ActivarAsync(empresaId, zonaId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Desactiva una zona. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost("{zonaId:int}/desactivar")]
    public async Task<IActionResult> DesactivarAsync(int empresaId, int zonaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _zonaServicio.DesactivarAsync(empresaId, zonaId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }
}
