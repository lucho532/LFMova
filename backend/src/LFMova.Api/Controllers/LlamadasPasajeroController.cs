using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.Llamadas;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone el registro de las llamadas del conductor a un pasajero. Solo el
/// conductor asignado puede registrarlas; el historial lo pueden consultar
/// los tres implicados: ese conductor, el empleado que es el pasajero y un
/// coordinador de la empresa. No contiene lógica de negocio: delega en
/// <see cref="IRegistroLlamadaServicio"/>.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/jornadas/{jornadaId:int}/servicios/{servicioId:int}/pasajeros/{servicioPasajeroId:int}/llamadas")]
[Authorize]
public class LlamadasPasajeroController : ControllerBase
{
    private readonly IRegistroLlamadaServicio _llamadaServicio;
    private readonly IChatServicio _chatServicio;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public LlamadasPasajeroController(IRegistroLlamadaServicio llamadaServicio, IChatServicio chatServicio)
    {
        _llamadaServicio = llamadaServicio;
        _chatServicio = chatServicio;
    }

    /// <summary>Deja constancia de que el conductor acaba de pulsar "Llamar" sobre el pasajero.</summary>
    [HttpPost]
    public async Task<ActionResult<RegistroLlamadaDto>> RegistrarAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, RegistrarLlamadaDto datos)
    {
        try
        {
            var (_, usuarioIdConductor) = await _chatServicio.ObtenerParticipantesAsync(empresaId, servicioPasajeroId);
            if (usuarioIdConductor is null || !User.EsUsuario(usuarioIdConductor.Value))
            {
                return Forbid();
            }

            return Ok(await _llamadaServicio.RegistrarAsync(empresaId, servicioPasajeroId, datos));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Guarda la duración aproximada de una llamada, medida al volver el conductor a la aplicación.</summary>
    [HttpPut("{registroLlamadaId:int}/duracion")]
    public async Task<ActionResult<RegistroLlamadaDto>> RegistrarDuracionAsync(
        int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, int registroLlamadaId, DuracionLlamadaDto datos)
    {
        try
        {
            var (_, usuarioIdConductor) = await _chatServicio.ObtenerParticipantesAsync(empresaId, servicioPasajeroId);
            if (usuarioIdConductor is null || !User.EsUsuario(usuarioIdConductor.Value))
            {
                return Forbid();
            }

            return Ok(await _llamadaServicio.RegistrarDuracionAsync(empresaId, servicioPasajeroId, registroLlamadaId, datos.Segundos));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Consulta las llamadas hechas al pasajero. La ven el conductor, el propio pasajero y los coordinadores de la empresa.</summary>
    [HttpGet]
    public async Task<ActionResult<List<RegistroLlamadaDto>>> ObtenerAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId)
    {
        try
        {
            var (usuarioIdEmpleado, usuarioIdConductor) = await _chatServicio.ObtenerParticipantesAsync(empresaId, servicioPasajeroId);
            var esImplicado = (usuarioIdEmpleado is not null && User.EsUsuario(usuarioIdEmpleado.Value))
                || (usuarioIdConductor is not null && User.EsUsuario(usuarioIdConductor.Value))
                || User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId);
            if (!esImplicado)
            {
                return Forbid();
            }

            return Ok(await _llamadaServicio.ObtenerPorServicioPasajeroAsync(empresaId, servicioPasajeroId));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }
}
