using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.Chat;
using LFMova.Application.Interfaces;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone la conversación individual entre el conductor y el empleado de un
/// pasajero (ver <c>AGENTS.md</c> §30). No contiene lógica de negocio: delega
/// en <see cref="IChatServicio"/> y solo deja entrar a los dos participantes
/// de ese transporte. No implementa chat grupal.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/jornadas/{jornadaId:int}/servicios/{servicioId:int}/pasajeros")]
[Authorize]
public class MensajesPasajeroController : ControllerBase
{
    private readonly IChatServicio _chatServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public MensajesPasajeroController(IChatServicio chatServicio)
    {
        _chatServicio = chatServicio;
    }

    /// <summary>
    /// Consulta los mensajes de la conversación individual del pasajero (ver
    /// <c>tasks.md</c> T087). Solo el conductor y el empleado involucrados en
    /// ese transporte pueden acceder.
    /// </summary>
    [HttpGet("{servicioPasajeroId:int}/mensajes")]
    public async Task<ActionResult<List<MensajeDto>>> ObtenerMensajesAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId)
    {
        try
        {
            if (!await EsParticipanteDeLaConversacionAsync(empresaId, servicioPasajeroId))
            {
                return Forbid();
            }

            return Ok(await _chatServicio.ObtenerMensajesAsync(empresaId, servicioPasajeroId));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Envía un mensaje en la conversación individual del pasajero (ver
    /// <c>tasks.md</c> T087). Solo el conductor y el empleado involucrados en
    /// ese transporte pueden enviar mensajes. No implementa chat grupal.
    /// </summary>
    [HttpPost("{servicioPasajeroId:int}/mensajes")]
    public async Task<ActionResult<MensajeDto>> EnviarMensajeAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, EnviarMensajeDto datos)
    {
        try
        {
            if (!await EsParticipanteDeLaConversacionAsync(empresaId, servicioPasajeroId))
            {
                return Forbid();
            }

            var mensaje = await _chatServicio.EnviarMensajeAsync(empresaId, servicioPasajeroId, User.ObtenerUsuarioId(), datos);
            return Ok(mensaje);
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    private async Task<bool> EsParticipanteDeLaConversacionAsync(int empresaId, int servicioPasajeroId)
    {
        var (usuarioIdEmpleado, usuarioIdConductor) = await _chatServicio.ObtenerParticipantesAsync(empresaId, servicioPasajeroId);
        return (usuarioIdEmpleado is not null && User.EsUsuario(usuarioIdEmpleado.Value))
            || (usuarioIdConductor is not null && User.EsUsuario(usuarioIdConductor.Value));
    }
}
