using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportApp.Api.Configuration;
using TransportApp.Application.DTOs.Invitaciones;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;

namespace TransportApp.Api.Controllers;

/// <summary>
/// Expone la invitación por correo a una empresa: el coordinador invita y
/// consulta sus invitaciones; la persona invitada ve el detalle desde el
/// enlace (sin sesión) y la acepta (con sesión). No contiene lógica de
/// negocio: delega en <see cref="IInvitacionEmpresaServicio"/>.
/// </summary>
[ApiController]
[Authorize]
public class InvitacionesController : ControllerBase
{
    private readonly IInvitacionEmpresaServicio _invitacionServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public InvitacionesController(IInvitacionEmpresaServicio invitacionServicio)
    {
        _invitacionServicio = invitacionServicio;
    }

    /// <summary>
    /// Envía una invitación por correo a una cédula para unirse a la empresa.
    /// Solo un coordinador de esa empresa. La respuesta nunca indica si la
    /// cédula ya tenía cuenta ni a qué correo se envió.
    /// </summary>
    [HttpPost("api/empresas/{empresaId:int}/invitaciones")]
    public async Task<IActionResult> InvitarAsync(int empresaId, CrearInvitacionEmpresaDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _invitacionServicio.InvitarAsync(empresaId, datos, User.ObtenerUsuarioId());
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Consulta las invitaciones enviadas por la empresa. Solo un coordinador de esa empresa.</summary>
    [HttpGet("api/empresas/{empresaId:int}/invitaciones")]
    public async Task<ActionResult<List<InvitacionEmpresaDto>>> ObtenerPorEmpresaAsync(int empresaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _invitacionServicio.ObtenerPorEmpresaAsync(empresaId));
    }

    /// <summary>
    /// Detalle de una invitación para quien abre el enlace del correo
    /// (<c>?token=</c>). No requiere sesión: el token solo lo tiene quien
    /// recibió el correo.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("api/invitaciones/detalle")]
    public async Task<ActionResult<DetalleInvitacionDto>> ObtenerDetalleAsync([FromQuery] string token)
    {
        try
        {
            return Ok(await _invitacionServicio.ObtenerDetalleAsync(token));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>La persona autenticada (dueña de la cédula invitada) acepta la invitación.</summary>
    [HttpPost("api/invitaciones/aceptar")]
    public async Task<IActionResult> AceptarAsync(AceptarInvitacionDto datos)
    {
        try
        {
            await _invitacionServicio.AceptarAsync(datos.Token, User.ObtenerUsuarioId());
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }
}
