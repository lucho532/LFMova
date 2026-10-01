using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.Empleados;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone la gestión directa de empleados de una empresa: consulta,
/// actualización de datos actuales y activación/desactivación. No expone
/// ninguna operación para cambiar la empresa de un empleado: eso se resuelve
/// exclusivamente mediante la importación de Excel (ver <c>spec.md</c> §39).
/// No contiene lógica de negocio: delega en <see cref="IEmpleadoServicio"/> y
/// verifica el contexto de empresa antes de invocarlo.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/empleados")]
[Authorize]
public class EmpleadosController : ControllerBase
{
    private readonly IEmpleadoServicio _empleadoServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public EmpleadosController(IEmpleadoServicio empleadoServicio)
    {
        _empleadoServicio = empleadoServicio;
    }

    /// <summary>
    /// Consulta los empleados de la empresa. Solo un coordinador de esa
    /// empresa puede hacerlo.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<EmpleadoDto>>> ObtenerPorEmpresaAsync(int empresaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _empleadoServicio.ObtenerPorEmpresaAsync(empresaId));
    }

    /// <summary>Consulta un empleado concreto de la empresa.</summary>
    [HttpGet("{empleadoId:int}")]
    public async Task<ActionResult<EmpleadoDto>> ObtenerPorIdAsync(int empresaId, int empleadoId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        var empleado = await _empleadoServicio.ObtenerPorIdAsync(empresaId, empleadoId);
        return empleado is null ? NotFound() : Ok(empleado);
    }

    /// <summary>
    /// Actualiza los datos actuales de un empleado. Solo un coordinador de
    /// esa empresa puede hacerlo.
    /// </summary>
    [HttpPut("{empleadoId:int}")]
    public async Task<IActionResult> ActualizarAsync(int empresaId, int empleadoId, ActualizarEmpleadoDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _empleadoServicio.ActualizarAsync(empresaId, empleadoId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }
}
