using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportApp.Api.Configuration;
using TransportApp.Application.DTOs.Conductores;
using TransportApp.Application.DTOs.UnidadesOperativas;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;

namespace TransportApp.Api.Controllers;

/// <summary>
/// Expone la creación y gestión de unidades operativas (conductor +
/// vehículo). Accesible por el propio conductor autenticado o por un
/// coordinador de alguna empresa con la que el conductor tenga una
/// vinculación activa. No contiene lógica de negocio: delega en
/// <see cref="IUnidadOperativaServicio"/>.
/// </summary>
[ApiController]
[Route("api/conductores/{conductorId:int}/unidades-operativas")]
[Authorize]
public class UnidadesOperativasController : ControllerBase
{
    private readonly IUnidadOperativaServicio _unidadOperativaServicio;
    private readonly IConductorServicio _conductorServicio;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public UnidadesOperativasController(
        IUnidadOperativaServicio unidadOperativaServicio,
        IConductorServicio conductorServicio)
    {
        _unidadOperativaServicio = unidadOperativaServicio;
        _conductorServicio = conductorServicio;
    }

    /// <summary>
    /// Crea una unidad operativa a partir del conductor y el vehículo
    /// indicados.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UnidadOperativaDto>> CrearAsync(int conductorId, CrearUnidadOperativaDto datos)
    {
        if (await ObtenerConductorAutorizadoOFallarAsync(conductorId) is null)
        {
            return Forbid();
        }

        try
        {
            var unidad = await _unidadOperativaServicio.CrearAsync(conductorId, datos);
            return CreatedAtAction(nameof(ObtenerPorConductorAsync), new { conductorId }, unidad);
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Consulta las unidades operativas del conductor.</summary>
    [HttpGet]
    public async Task<ActionResult<List<UnidadOperativaDto>>> ObtenerPorConductorAsync(int conductorId)
    {
        if (await ObtenerConductorAutorizadoOFallarAsync(conductorId) is null)
        {
            return Forbid();
        }

        return Ok(await _unidadOperativaServicio.ObtenerPorConductorAsync(conductorId));
    }

    /// <summary>Activa una unidad operativa del conductor.</summary>
    [HttpPost("{unidadOperativaId:int}/activar")]
    public async Task<IActionResult> ActivarAsync(int conductorId, int unidadOperativaId)
    {
        if (await ObtenerConductorAutorizadoOFallarAsync(conductorId) is null)
        {
            return Forbid();
        }

        try
        {
            await _unidadOperativaServicio.ActivarAsync(conductorId, unidadOperativaId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Desactiva una unidad operativa del conductor.</summary>
    [HttpPost("{unidadOperativaId:int}/desactivar")]
    public async Task<IActionResult> DesactivarAsync(int conductorId, int unidadOperativaId)
    {
        if (await ObtenerConductorAutorizadoOFallarAsync(conductorId) is null)
        {
            return Forbid();
        }

        try
        {
            await _unidadOperativaServicio.DesactivarAsync(conductorId, unidadOperativaId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Obtiene el conductor indicado si el usuario autenticado está
    /// autorizado a gestionarlo (es el propio conductor, o es coordinador de
    /// alguna empresa con la que el conductor tenga una vinculación activa);
    /// en caso contrario, <c>null</c>.
    /// </summary>
    private async Task<ConductorDto?> ObtenerConductorAutorizadoOFallarAsync(int conductorId)
    {
        var conductor = await _conductorServicio.ObtenerPorIdAsync(conductorId);
        if (conductor is null)
        {
            return null;
        }

        var autorizado = User.EsUsuario(conductor.UsuarioId)
            || conductor.EmpresaIdsVinculadosActivos.Any(empresaId => User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId));

        return autorizado ? conductor : null;
    }
}
