using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone lo que ocurre con un pasajero durante su transporte: la respuesta
/// del empleado (confirma o avisa que no asistirá) y lo que registra el
/// conductor (orden de recogida, llegada y resultado). No contiene lógica de
/// negocio: delega en <see cref="IServicioPasajeroServicio"/> y verifica quién
/// hace cada operación antes de invocarlo.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/jornadas/{jornadaId:int}/servicios/{servicioId:int}/pasajeros")]
[Authorize]
public class ParticipacionPasajeroController : ControllerBase
{
    private readonly IServicioPasajeroServicio _servicioPasajeroServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public ParticipacionPasajeroController(IServicioPasajeroServicio servicioPasajeroServicio)
    {
        _servicioPasajeroServicio = servicioPasajeroServicio;
    }

    /// <summary>Confirma la asistencia del pasajero, opcionalmente actualizando su dirección de recogida. Lo puede hacer un coordinador o el propio empleado.</summary>
    [HttpPost("{servicioPasajeroId:int}/confirmar")]
    public async Task<IActionResult> ConfirmarAsync(int empresaId, int servicioId, int servicioPasajeroId, ConfirmarServicioPasajeroDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId) && !await EsElPropioEmpleadoAsync(empresaId, servicioPasajeroId))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.ConfirmarAsync(empresaId, servicioPasajeroId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Marca que el pasajero no asistirá: notifica al conductor responsable
    /// del servicio.
    /// </summary>
    [HttpPost("{servicioPasajeroId:int}/no-asistira")]
    public async Task<IActionResult> MarcarNoAsistiraAsync(int empresaId, int servicioId, int servicioPasajeroId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId) && !await EsElPropioEmpleadoAsync(empresaId, servicioPasajeroId))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.MarcarNoAsistiraAsync(empresaId, servicioPasajeroId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Modifica el orden operativo del pasajero dentro del servicio. Lo puede hacer un coordinador de la empresa o el conductor asignado.</summary>
    [HttpPut("{servicioPasajeroId:int}/orden")]
    public async Task<IActionResult> ReordenarAsync(int empresaId, int servicioId, int servicioPasajeroId, ReordenarServicioPasajeroDto datos)
    {
        var usuarioIdConductor = await _servicioPasajeroServicio.ObtenerUsuarioIdConductorAsync(empresaId, servicioPasajeroId);
        var esConductorAsignado = usuarioIdConductor is not null && User.EsUsuario(usuarioIdConductor.Value);
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId) && !esConductorAsignado)
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.ReordenarAsync(empresaId, servicioPasajeroId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Registra manualmente que el conductor llegó al punto de recogida del
    /// pasajero (ver <c>tasks.md</c> T084). Es la alternativa manual mientras
    /// la detección automática por geolocalización permanezca pendiente de
    /// definición. Solo el conductor de la unidad operativa asignada al
    /// servicio puede registrarla.
    /// </summary>
    [HttpPost("{servicioPasajeroId:int}/marcar-llegada")]
    public async Task<IActionResult> MarcarLlegadaAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId)
    {
        var usuarioIdConductor = await _servicioPasajeroServicio.ObtenerUsuarioIdConductorAsync(empresaId, servicioPasajeroId);
        if (usuarioIdConductor is null || !User.EsUsuario(usuarioIdConductor.Value))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.MarcarLlegadaAsync(empresaId, servicioPasajeroId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Registra el resultado del procesamiento del pasajero tras la espera,
    /// o su avance dentro del vehículo (ver <c>tasks.md</c> T085/T090). Solo
    /// el conductor de la unidad operativa asignada al servicio puede
    /// registrarlo.
    /// </summary>
    [HttpPut("{servicioPasajeroId:int}/estado")]
    public async Task<IActionResult> CambiarEstadoAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, CambiarEstadoServicioPasajeroDto datos)
    {
        var usuarioIdConductor = await _servicioPasajeroServicio.ObtenerUsuarioIdConductorAsync(empresaId, servicioPasajeroId);
        if (usuarioIdConductor is null || !User.EsUsuario(usuarioIdConductor.Value))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.CambiarEstadoAsync(empresaId, servicioPasajeroId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    private async Task<bool> EsElPropioEmpleadoAsync(int empresaId, int servicioPasajeroId)
    {
        var usuarioIdEmpleado = await _servicioPasajeroServicio.ObtenerUsuarioIdEmpleadoAsync(empresaId, servicioPasajeroId);
        return usuarioIdEmpleado is not null && User.EsUsuario(usuarioIdEmpleado.Value);
    }
}
