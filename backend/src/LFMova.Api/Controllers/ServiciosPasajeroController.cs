using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone la asignación de programaciones a un servicio, la consulta de sus
/// pasajeros y los cambios con los que el coordinador reorganiza una ruta
/// (quitar, corregir dirección, mover). No contiene lógica de negocio: delega en
/// <see cref="IServicioPasajeroServicio"/> y verifica el contexto de empresa
/// antes de invocarlo.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/jornadas/{jornadaId:int}/servicios/{servicioId:int}/pasajeros")]
[Authorize]
public class ServiciosPasajeroController : ControllerBase
{
    private readonly IServicioPasajeroServicio _servicioPasajeroServicio;
    private readonly IServicioServicio _servicioServicio;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ServiciosPasajeroController(IServicioPasajeroServicio servicioPasajeroServicio, IServicioServicio servicioServicio)
    {
        _servicioPasajeroServicio = servicioPasajeroServicio;
        _servicioServicio = servicioServicio;
    }

    /// <summary>Asigna una programación al servicio, creando su pasajero.</summary>
    [HttpPost]
    public async Task<ActionResult<ServicioPasajeroDto>> CrearAsync(int empresaId, int jornadaId, int servicioId, CrearServicioPasajeroDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            var pasajero = await _servicioPasajeroServicio.CrearAsync(empresaId, servicioId, datos);
            return CreatedAtAction(nameof(ObtenerPorServicioAsync), new { empresaId, jornadaId, servicioId }, pasajero);
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Consulta los pasajeros del servicio, incluyendo nombre y teléfono de
    /// contacto de cada empleado (ver <c>tasks.md</c> T086). Accesible para
    /// un coordinador de la empresa o para el conductor de la unidad
    /// operativa asignada al servicio, que la usa para ejecutar la ruta.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ServicioPasajeroDto>>> ObtenerPorServicioAsync(int empresaId, int jornadaId, int servicioId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            var usuarioIdConductor = await _servicioServicio.ObtenerUsuarioIdConductorAsignadoAsync(empresaId, servicioId);
            if (usuarioIdConductor is null || !User.EsUsuario(usuarioIdConductor.Value))
            {
                return Forbid();
            }
        }

        try
        {
            return Ok(await _servicioPasajeroServicio.ObtenerPorServicioAsync(empresaId, servicioId));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>El coordinador elimina por completo al pasajero de la ruta (para cuando se agregó por error); no queda registro de que estuvo ahí.</summary>
    [HttpDelete("{servicioPasajeroId:int}")]
    public async Task<IActionResult> EliminarAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.EliminarAsync(empresaId, servicioPasajeroId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>El coordinador corrige la dirección de recogida de este servicio puntual (por ejemplo, un dato mal pegado al crear la ruta).</summary>
    [HttpPut("{servicioPasajeroId:int}/direccion")]
    public async Task<IActionResult> EditarDireccionAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, EditarDireccionServicioPasajeroDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.EditarDireccionAsync(empresaId, servicioPasajeroId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>El coordinador arrastra al pasajero a otra ruta ya existente de la misma sede, fecha, hora y tipo.</summary>
    [HttpPut("{servicioPasajeroId:int}/mover")]
    public async Task<IActionResult> MoverAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, MoverServicioPasajeroDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.MoverAsync(empresaId, servicioPasajeroId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }
}
