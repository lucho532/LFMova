using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportApp.Api.Configuration;
using TransportApp.Application.DTOs.Jornadas;
using TransportApp.Application.DTOs.Servicios;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;

namespace TransportApp.Api.Controllers;

/// <summary>
/// Expone la administración de jornadas de una empresa. No contiene lógica
/// de negocio: delega en <see cref="IJornadaServicio"/> y verifica el
/// contexto de empresa antes de invocarlo.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/jornadas")]
[Authorize]
public class JornadasController : ControllerBase
{
    private readonly IJornadaServicio _jornadaServicio;
    private readonly IServicioServicio _servicioServicio;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public JornadasController(IJornadaServicio jornadaServicio, IServicioServicio servicioServicio)
    {
        _jornadaServicio = jornadaServicio;
        _servicioServicio = servicioServicio;
    }

    /// <summary>
    /// Rutas que muestra la pantalla de Programación, de todas las jornadas:
    /// las que falta enviar (de cualquier fecha) y las publicadas o en curso
    /// desde <c>?desde=AAAA-MM-DD</c>. Solo un coordinador de esa empresa.
    /// </summary>
    [HttpGet("servicios-pendientes")]
    public async Task<ActionResult<List<ServicioDto>>> ObtenerServiciosPendientesAsync(int empresaId, [FromQuery] DateOnly desde)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _servicioServicio.ObtenerPendientesDeProgramacionAsync(empresaId, desde));
    }

    /// <summary>Crea una jornada para la empresa. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost]
    public async Task<ActionResult<JornadaDto>> CrearAsync(int empresaId, CrearJornadaDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            var jornada = await _jornadaServicio.CrearAsync(empresaId, datos);
            return CreatedAtAction(nameof(ObtenerPorIdAsync), new { empresaId, jornadaId = jornada.JornadaId }, jornada);
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Consulta las jornadas de la empresa. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpGet]
    public async Task<ActionResult<List<JornadaDto>>> ObtenerPorEmpresaAsync(int empresaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _jornadaServicio.ObtenerPorEmpresaAsync(empresaId));
    }

    /// <summary>Consulta una jornada concreta de la empresa.</summary>
    [HttpGet("{jornadaId:int}")]
    public async Task<ActionResult<JornadaDto>> ObtenerPorIdAsync(int empresaId, int jornadaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        var jornada = await _jornadaServicio.ObtenerPorIdAsync(empresaId, jornadaId);
        return jornada is null ? NotFound() : Ok(jornada);
    }

    /// <summary>
    /// Publica la jornada. Solo un coordinador de esa empresa puede hacerlo.
    /// </summary>
    [HttpPost("{jornadaId:int}/publicar")]
    public async Task<IActionResult> PublicarAsync(int empresaId, int jornadaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _jornadaServicio.PublicarAsync(empresaId, jornadaId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Deshace el reparto automático de la jornada: borra los servicios que
    /// todavía no se publicaron y deja sus pasajeros listos para repartirse
    /// de nuevo. Solo un coordinador de esa empresa puede hacerlo.
    /// </summary>
    [HttpPost("{jornadaId:int}/deshacer-reparto")]
    public async Task<ActionResult<DeshacerRepartoDto>> DeshacerRepartoAsync(int empresaId, int jornadaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _jornadaServicio.DeshacerRepartoAsync(empresaId, jornadaId));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Elimina todo rastro de programación de la jornada (servicios,
    /// pasajeros y programaciones sin asignar de esa fecha) para poder
    /// volver a importar el Excel desde cero. Solo un coordinador de esa
    /// empresa puede hacerlo.
    /// </summary>
    [HttpPost("{jornadaId:int}/eliminar-rastro")]
    public async Task<ActionResult<EliminarRastroDto>> EliminarRastroAsync(int empresaId, int jornadaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _jornadaServicio.EliminarRastroAsync(empresaId, jornadaId));
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }
}
