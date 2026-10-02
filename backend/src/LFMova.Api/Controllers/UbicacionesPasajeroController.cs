using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone las ubicaciones de recogida de un pasajero: la que comparte el
/// empleado, la que guarda el conductor y el historial de ubicaciones
/// anteriores. No contiene lógica de negocio: delega en
/// <see cref="IServicioPasajeroServicio"/> y verifica quién hace cada
/// operación antes de invocarlo.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/jornadas/{jornadaId:int}/servicios/{servicioId:int}/pasajeros")]
[Authorize]
public class UbicacionesPasajeroController : ControllerBase
{
    private readonly IServicioPasajeroServicio _servicioPasajeroServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public UbicacionesPasajeroController(IServicioPasajeroServicio servicioPasajeroServicio)
    {
        _servicioPasajeroServicio = servicioPasajeroServicio;
    }

    /// <summary>
    /// Actualiza la ubicación compartida por el empleado para este servicio
    /// (ver <c>tasks.md</c> T088). Solo el propio empleado dueño del pasajero
    /// puede compartir su ubicación.
    /// </summary>
    [HttpPut("{servicioPasajeroId:int}/ubicacion")]
    public async Task<IActionResult> CompartirUbicacionAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, CompartirUbicacionDto datos)
    {
        var usuarioIdEmpleado = await _servicioPasajeroServicio.ObtenerUsuarioIdEmpleadoAsync(empresaId, servicioPasajeroId);
        if (usuarioIdEmpleado is null || !User.EsUsuario(usuarioIdEmpleado.Value))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.CompartirUbicacionAsync(empresaId, servicioPasajeroId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Guarda el punto GPS exacto de la recogida y lo conserva en el historial
    /// del empleado. Solo el conductor asignado al servicio puede hacerlo.
    /// </summary>
    [HttpPut("{servicioPasajeroId:int}/ubicacion-recogida")]
    public async Task<IActionResult> GuardarUbicacionRecogidaAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, CompartirUbicacionDto datos)
    {
        var usuarioIdConductor = await _servicioPasajeroServicio.ObtenerUsuarioIdConductorAsync(empresaId, servicioPasajeroId);
        if (usuarioIdConductor is null || !User.EsUsuario(usuarioIdConductor.Value))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.GuardarUbicacionRecogidaAsync(empresaId, servicioPasajeroId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Consulta las ubicaciones de recogida anteriores del empleado del
    /// pasajero, para poder navegar a ellas. Solo el conductor asignado o un
    /// coordinador de la empresa.
    /// </summary>
    [HttpGet("{servicioPasajeroId:int}/ubicaciones-anteriores")]
    public async Task<ActionResult<List<UbicacionAnteriorDto>>> ObtenerUbicacionesAnterioresAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId)
    {
        var usuarioIdConductor = await _servicioPasajeroServicio.ObtenerUsuarioIdConductorAsync(empresaId, servicioPasajeroId);
        var esConductorAsignado = usuarioIdConductor is not null && User.EsUsuario(usuarioIdConductor.Value);
        if (!esConductorAsignado && !User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _servicioPasajeroServicio.ObtenerUbicacionesAnterioresAsync(empresaId, servicioPasajeroId));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Olvida la ubicación guardada del empleado del pasajero, para que la
    /// próxima recogida guarde una nueva. Solo el conductor asignado o un
    /// coordinador de la empresa.
    /// </summary>
    [HttpDelete("{servicioPasajeroId:int}/ubicaciones-anteriores")]
    public async Task<IActionResult> EliminarUbicacionGuardadaAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId)
    {
        var usuarioIdConductor = await _servicioPasajeroServicio.ObtenerUsuarioIdConductorAsync(empresaId, servicioPasajeroId);
        var esConductorAsignado = usuarioIdConductor is not null && User.EsUsuario(usuarioIdConductor.Value);
        if (!esConductorAsignado && !User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.EliminarUbicacionGuardadaAsync(empresaId, servicioPasajeroId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }
}
