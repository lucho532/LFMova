using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.Cuenta;
using LFMova.Application.Interfaces;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone la consulta y edición de la propia cuenta del usuario autenticado
/// y el cambio de su contraseña. El usuario se identifica siempre por su
/// token, nunca por un identificador recibido en la ruta. No contiene lógica
/// de negocio: delega en <see cref="ICuentaServicio"/>.
/// </summary>
[ApiController]
[Route("api/cuenta")]
[Authorize]
public class CuentaController : ControllerBase
{
    private readonly ICuentaServicio _cuentaServicio;
    private readonly IEliminacionPersonaServicio _eliminacionServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public CuentaController(ICuentaServicio cuentaServicio, IEliminacionPersonaServicio eliminacionServicio)
    {
        _cuentaServicio = cuentaServicio;
        _eliminacionServicio = eliminacionServicio;
    }

    /// <summary>Consulta los datos de la cuenta del usuario autenticado.</summary>
    [HttpGet]
    public async Task<ActionResult<CuentaDto>> ObtenerAsync()
    {
        try
        {
            return Ok(await _cuentaServicio.ObtenerAsync(User.ObtenerUsuarioId()));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Actualiza el nombre y el teléfono del usuario autenticado.</summary>
    [HttpPut]
    public async Task<ActionResult<CuentaDto>> ActualizarAsync(ActualizarCuentaDto datos)
    {
        try
        {
            return Ok(await _cuentaServicio.ActualizarAsync(User.ObtenerUsuarioId(), datos));
        }
        catch (InvalidOperationException excepcion)
        {
            return BadRequest(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Cambia la contraseña del usuario autenticado.</summary>
    [HttpPost("cambiar-contrasena")]
    public async Task<IActionResult> CambiarContrasenaAsync(CambiarContrasenaDto datos)
    {
        try
        {
            await _cuentaServicio.CambiarContrasenaAsync(User.ObtenerUsuarioId(), datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return BadRequest(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// La persona autenticada elimina su propia cuenta y todo su rastro,
    /// confirmando con su contraseña. No se puede deshacer.
    /// </summary>
    [HttpPost("eliminar")]
    public async Task<IActionResult> EliminarAsync(EliminarCuentaDto datos)
    {
        try
        {
            await _eliminacionServicio.EliminarPropiaCuentaAsync(User.ObtenerUsuarioId(), datos.Contrasena);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return BadRequest(new { mensaje = excepcion.Message });
        }
    }
}
