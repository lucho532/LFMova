using Microsoft.AspNetCore.Mvc;
using TransportApp.Application.DTOs.Autenticacion;
using TransportApp.Application.Interfaces;

namespace TransportApp.Api.Controllers;

/// <summary>
/// Expone el inicio de sesión (cédula o correo y contraseña), el
/// autorregistro, la confirmación de correo y la recuperación de contraseña.
/// No contiene lógica de negocio: delega en los servicios de aplicación.
/// </summary>
[ApiController]
[Route("api/autenticacion")]
public class AutenticacionController : ControllerBase
{
    private readonly IServicioAutenticacion _servicioAutenticacion;
    private readonly IRegistroServicio _registroServicio;
    private readonly IRecuperacionContrasenaServicio _recuperacionContrasenaServicio;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public AutenticacionController(
        IServicioAutenticacion servicioAutenticacion,
        IRegistroServicio registroServicio,
        IRecuperacionContrasenaServicio recuperacionContrasenaServicio)
    {
        _servicioAutenticacion = servicioAutenticacion;
        _registroServicio = registroServicio;
        _recuperacionContrasenaServicio = recuperacionContrasenaServicio;
    }

    /// <summary>Inicia sesión mediante cédula o correo y contraseña y devuelve un token JWT.</summary>
    [HttpPost("iniciar-sesion")]
    public async Task<ActionResult<RespuestaAutenticacionDto>> IniciarSesionAsync(IniciarSesionDto datos)
    {
        try
        {
            var respuesta = await _servicioAutenticacion.IniciarSesionAsync(datos);
            return Ok(respuesta);
        }
        catch (InvalidOperationException excepcion)
        {
            return Unauthorized(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Registra una cuenta nueva con el rol EMPLEADO (sin empresa asignada) y
    /// envía un correo para confirmarla, salvo que venga de una invitación con
    /// el mismo correo (ver <see cref="ResultadoRegistroDto"/>). No requiere
    /// autenticación previa.
    /// </summary>
    [HttpPost("registrarse")]
    public async Task<ActionResult<ResultadoRegistroDto>> RegistrarseAsync(RegistrarUsuarioDto datos)
    {
        try
        {
            return Ok(await _registroServicio.RegistrarAsync(datos));
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Confirma el correo de una cuenta mediante el token enviado por correo.</summary>
    [HttpPost("confirmar-correo")]
    public async Task<IActionResult> ConfirmarCorreoAsync(ConfirmarCorreoDto datos)
    {
        try
        {
            await _registroServicio.ConfirmarCorreoAsync(datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return BadRequest(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Solicita un enlace de recuperación de contraseña. Siempre responde
    /// 204, exista o no la cuenta, para no revelar qué cédulas o correos
    /// están registrados.
    /// </summary>
    [HttpPost("solicitar-recuperacion")]
    public async Task<IActionResult> SolicitarRecuperacionAsync(SolicitarRecuperacionDto datos)
    {
        try
        {
            await _recuperacionContrasenaServicio.SolicitarAsync(datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return BadRequest(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Establece la contraseña de una cuenta mediante el token enviado por
    /// correo (recuperación o contraseña inicial de una cuenta invitada).
    /// </summary>
    [HttpPost("restablecer-contrasena")]
    public async Task<IActionResult> RestablecerContrasenaAsync(RestablecerContrasenaDto datos)
    {
        try
        {
            await _recuperacionContrasenaServicio.RestablecerAsync(datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return BadRequest(new { mensaje = excepcion.Message });
        }
    }
}
