using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.Programaciones;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone la administración de programaciones de transporte de una empresa.
/// No contiene lógica de negocio: delega en
/// <see cref="IProgramacionTransporteServicio"/> y verifica el contexto de
/// empresa antes de invocarlo.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/programaciones")]
[Authorize]
public class ProgramacionesController : ControllerBase
{
    private readonly IProgramacionTransporteServicio _programacionServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public ProgramacionesController(IProgramacionTransporteServicio programacionServicio)
    {
        _programacionServicio = programacionServicio;
    }

    /// <summary>Crea una programación de transporte para la empresa. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost]
    public async Task<ActionResult<ProgramacionDto>> CrearAsync(int empresaId, CrearProgramacionDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            var programacion = await _programacionServicio.CrearAsync(empresaId, datos);
            return CreatedAtAction(nameof(ObtenerPorIdAsync), new { empresaId, programacionTransporteId = programacion.ProgramacionTransporteId }, programacion);
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Consulta las programaciones de la empresa. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ProgramacionDto>>> ObtenerPorEmpresaAsync(int empresaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _programacionServicio.ObtenerPorEmpresaAsync(empresaId));
    }

    /// <summary>Consulta una programación concreta de la empresa.</summary>
    [HttpGet("{programacionTransporteId:int}")]
    public async Task<ActionResult<ProgramacionDto>> ObtenerPorIdAsync(int empresaId, int programacionTransporteId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        var programacion = await _programacionServicio.ObtenerPorIdAsync(empresaId, programacionTransporteId);
        return programacion is null ? NotFound() : Ok(programacion);
    }

    /// <summary>Actualiza una programación de la empresa. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPut("{programacionTransporteId:int}")]
    public async Task<IActionResult> ActualizarAsync(int empresaId, int programacionTransporteId, ActualizarProgramacionDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _programacionServicio.ActualizarAsync(empresaId, programacionTransporteId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }
}
