using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.Conductores;
using LFMova.Application.DTOs.Vehiculos;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone el registro y gestión de vehículos de un conductor. Accesible por
/// el propio conductor autenticado o por un coordinador de alguna empresa
/// con la que el conductor tenga una vinculación activa. No contiene lógica
/// de negocio: delega en <see cref="IVehiculoServicio"/>.
/// </summary>
[ApiController]
[Route("api/conductores/{conductorId:int}/vehiculos")]
[Authorize]
public class VehiculosController : ControllerBase
{
    private readonly IVehiculoServicio _vehiculoServicio;
    private readonly IConductorServicio _conductorServicio;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public VehiculosController(IVehiculoServicio vehiculoServicio, IConductorServicio conductorServicio)
    {
        _vehiculoServicio = vehiculoServicio;
        _conductorServicio = conductorServicio;
    }

    /// <summary>Registra un nuevo vehículo perteneciente al conductor.</summary>
    [HttpPost]
    public async Task<ActionResult<VehiculoDto>> CrearAsync(int conductorId, CrearVehiculoDto datos)
    {
        var conductor = await ObtenerConductorAutorizadoOFallarAsync(conductorId);
        if (conductor is null)
        {
            return Forbid();
        }

        try
        {
            var vehiculo = await _vehiculoServicio.CrearAsync(conductorId, datos);
            return CreatedAtAction(nameof(ObtenerPorIdAsync), new { conductorId, vehiculoId = vehiculo.VehiculoId }, vehiculo);
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Consulta los vehículos del conductor.</summary>
    [HttpGet]
    public async Task<ActionResult<List<VehiculoDto>>> ObtenerPorConductorAsync(int conductorId)
    {
        if (await ObtenerConductorAutorizadoOFallarAsync(conductorId) is null)
        {
            return Forbid();
        }

        return Ok(await _vehiculoServicio.ObtenerPorConductorAsync(conductorId));
    }

    /// <summary>Consulta un vehículo concreto del conductor.</summary>
    [HttpGet("{vehiculoId:int}")]
    public async Task<ActionResult<VehiculoDto>> ObtenerPorIdAsync(int conductorId, int vehiculoId)
    {
        if (await ObtenerConductorAutorizadoOFallarAsync(conductorId) is null)
        {
            return Forbid();
        }

        var vehiculo = await _vehiculoServicio.ObtenerPorIdAsync(vehiculoId);
        if (vehiculo is null || vehiculo.ConductorId != conductorId)
        {
            return NotFound();
        }

        return Ok(vehiculo);
    }

    /// <summary>
    /// Actualiza los datos del vehículo (placa, marca, modelo, capacidad y
    /// vigencias). Lo puede hacer el propio conductor o un coordinador de una
    /// empresa a la que esté vinculado.
    /// </summary>
    [HttpPut("{vehiculoId:int}")]
    public async Task<ActionResult<VehiculoDto>> ActualizarAsync(int conductorId, int vehiculoId, CrearVehiculoDto datos)
    {
        if (await ObtenerConductorAutorizadoOFallarAsync(conductorId) is null)
        {
            return Forbid();
        }

        try
        {
            return Ok(await _vehiculoServicio.ActualizarAsync(conductorId, vehiculoId, datos));
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Activa un vehículo del conductor.</summary>
    [HttpPost("{vehiculoId:int}/activar")]
    public async Task<IActionResult> ActivarAsync(int conductorId, int vehiculoId)
    {
        if (await ObtenerConductorAutorizadoOFallarAsync(conductorId) is null)
        {
            return Forbid();
        }

        try
        {
            await _vehiculoServicio.ActivarAsync(conductorId, vehiculoId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Desactiva un vehículo del conductor. Si tiene una unidad operativa
    /// activa asociada, también se desactiva.
    /// </summary>
    [HttpPost("{vehiculoId:int}/desactivar")]
    public async Task<IActionResult> DesactivarAsync(int conductorId, int vehiculoId)
    {
        if (await ObtenerConductorAutorizadoOFallarAsync(conductorId) is null)
        {
            return Forbid();
        }

        try
        {
            await _vehiculoServicio.DesactivarAsync(conductorId, vehiculoId);
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
