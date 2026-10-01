using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.Notificaciones;
using LFMova.Application.Interfaces;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone la consulta y el marcado como leída de las notificaciones de un
/// usuario (ver <c>tasks.md</c> T075). Solo el propio usuario autenticado
/// puede acceder a sus notificaciones. No contiene lógica de negocio: delega
/// en <see cref="INotificacionServicio"/>.
/// </summary>
[ApiController]
[Route("api/usuarios/{usuarioId:int}/notificaciones")]
[Authorize]
public class NotificacionesController : ControllerBase
{
    private readonly INotificacionServicio _notificacionServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public NotificacionesController(INotificacionServicio notificacionServicio)
    {
        _notificacionServicio = notificacionServicio;
    }

    /// <summary>Consulta las notificaciones del usuario indicado, más recientes primero.</summary>
    [HttpGet]
    public async Task<ActionResult<List<NotificacionDto>>> ObtenerPorUsuarioAsync(int usuarioId)
    {
        if (!User.EsUsuario(usuarioId))
        {
            return Forbid();
        }

        return Ok(await _notificacionServicio.ObtenerPorUsuarioAsync(usuarioId));
    }

    /// <summary>Marca una notificación del usuario indicado como leída.</summary>
    [HttpPost("{notificacionId:int}/marcar-leida")]
    public async Task<IActionResult> MarcarComoLeidaAsync(int usuarioId, int notificacionId)
    {
        if (!User.EsUsuario(usuarioId))
        {
            return Forbid();
        }

        try
        {
            await _notificacionServicio.MarcarComoLeidaAsync(usuarioId, notificacionId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }
}
