using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.Sedes;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Autenticacion;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone la administración de sedes de una empresa. No contiene lógica de
/// negocio: delega en <see cref="ISedeServicio"/> y verifica el contexto de
/// empresa antes de invocarlo.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/sedes")]
[Authorize]
public class SedesController : ControllerBase
{
    private readonly ISedeServicio _sedeServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public SedesController(ISedeServicio sedeServicio)
    {
        _sedeServicio = sedeServicio;
    }

    /// <summary>Crea una sede para la empresa. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost]
    public async Task<ActionResult<SedeDto>> CrearAsync(int empresaId, CrearSedeDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        var sede = await _sedeServicio.CrearAsync(empresaId, datos);
        return CreatedAtAction(nameof(ObtenerPorIdAsync), new { empresaId, sedeId = sede.SedeId }, sede);
    }

    /// <summary>
    /// Consulta las sedes de la empresa. Accesible para el administrador de
    /// plataforma o para un coordinador de esa misma empresa.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SedeDto>>> ObtenerPorEmpresaAsync(int empresaId)
    {
        if (!EsAdministradorPlataforma() && !User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _sedeServicio.ObtenerPorEmpresaAsync(empresaId));
    }

    /// <summary>Consulta una sede concreta de la empresa.</summary>
    [HttpGet("{sedeId:int}")]
    public async Task<ActionResult<SedeDto>> ObtenerPorIdAsync(int empresaId, int sedeId)
    {
        if (!EsAdministradorPlataforma() && !User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        var sede = await _sedeServicio.ObtenerPorIdAsync(empresaId, sedeId);
        return sede is null ? NotFound() : Ok(sede);
    }

    /// <summary>Actualiza los datos actuales de una sede. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPut("{sedeId:int}")]
    public async Task<IActionResult> ActualizarAsync(int empresaId, int sedeId, ActualizarSedeDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _sedeServicio.ActualizarAsync(empresaId, sedeId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Activa una sede. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost("{sedeId:int}/activar")]
    public async Task<IActionResult> ActivarAsync(int empresaId, int sedeId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _sedeServicio.ActivarAsync(empresaId, sedeId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Desactiva una sede. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost("{sedeId:int}/desactivar")]
    public async Task<IActionResult> DesactivarAsync(int empresaId, int sedeId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _sedeServicio.DesactivarAsync(empresaId, sedeId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    private bool EsAdministradorPlataforma()
        => User.Claims.Any(c => c.Type == GeneradorTokenJwt.ClaimRol && c.Value == Rol.ADMINISTRADOR_PLATAFORMA.ToString());
}
