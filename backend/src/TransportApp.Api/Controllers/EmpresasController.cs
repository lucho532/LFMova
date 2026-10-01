using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportApp.Api.Configuration;
using TransportApp.Application.DTOs.Empresas;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;
using TransportApp.Infrastructure.Autenticacion;

namespace TransportApp.Api.Controllers;

/// <summary>
/// Expone la administración de empresas: creación, consulta,
/// activación/desactivación y asignación/revocación del rol
/// <c>COORDINADOR</c>. No contiene lógica de negocio: delega en
/// <see cref="IEmpresaServicio"/> e <see cref="IAsignacionCoordinadorServicio"/>,
/// y verifica el contexto de empresa antes de invocarlos.
/// </summary>
[ApiController]
[Route("api/empresas")]
[Authorize]
public class EmpresasController : ControllerBase
{
    private readonly IEmpresaServicio _empresaServicio;
    private readonly IAsignacionCoordinadorServicio _asignacionCoordinadorServicio;
    private readonly IPersonaServicio _personaServicio;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public EmpresasController(
        IEmpresaServicio empresaServicio,
        IAsignacionCoordinadorServicio asignacionCoordinadorServicio,
        IPersonaServicio personaServicio)
    {
        _empresaServicio = empresaServicio;
        _asignacionCoordinadorServicio = asignacionCoordinadorServicio;
        _personaServicio = personaServicio;
    }

    /// <summary>
    /// Crea una nueva empresa junto con su primer coordinador. Solo el
    /// administrador de plataforma puede hacerlo.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequiereAdministradorPlataforma")]
    public async Task<ActionResult<EmpresaDto>> CrearAsync(CrearEmpresaDto datos)
    {
        try
        {
            var empresa = await _empresaServicio.CrearAsync(datos);
            return CreatedAtAction(nameof(ObtenerPorIdAsync), new { empresaId = empresa.EmpresaId }, empresa);
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Consulta todas las empresas. Solo el administrador de plataforma puede hacerlo.</summary>
    [HttpGet]
    [Authorize(Policy = "RequiereAdministradorPlataforma")]
    public async Task<ActionResult<List<EmpresaDto>>> ObtenerTodasAsync()
    {
        return Ok(await _empresaServicio.ObtenerTodasAsync());
    }

    /// <summary>
    /// Consulta una empresa. Accesible para el administrador de plataforma
    /// (cualquier empresa) o para un coordinador de esa misma empresa.
    /// </summary>
    [HttpGet("{empresaId:int}")]
    public async Task<ActionResult<EmpresaDto>> ObtenerPorIdAsync(int empresaId)
    {
        if (!EsAdministradorPlataforma() && !User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        var empresa = await _empresaServicio.ObtenerPorIdAsync(empresaId);
        return empresa is null ? NotFound() : Ok(empresa);
    }

    /// <summary>Activa una empresa. Solo el administrador de plataforma puede hacerlo.</summary>
    [HttpPost("{empresaId:int}/activar")]
    [Authorize(Policy = "RequiereAdministradorPlataforma")]
    public async Task<IActionResult> ActivarAsync(int empresaId)
    {
        try
        {
            await _empresaServicio.ActivarAsync(empresaId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Desactiva una empresa. Solo el administrador de plataforma puede hacerlo.</summary>
    [HttpPost("{empresaId:int}/desactivar")]
    [Authorize(Policy = "RequiereAdministradorPlataforma")]
    public async Task<IActionResult> DesactivarAsync(int empresaId)
    {
        try
        {
            await _empresaServicio.DesactivarAsync(empresaId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Asigna el rol COORDINADOR de la empresa a un usuario (identificado por
    /// cédula). Accesible para el administrador de plataforma (primer
    /// coordinador) o para un coordinador ya activo de esa misma empresa
    /// (coordinadores adicionales). Un coordinador solo puede hacerlo con
    /// personas que ya forman parte de su empresa (o sin cuenta todavía, que
    /// se crea por invitación): a una persona registrada ajena a la empresa
    /// primero debe invitarla por correo.
    /// </summary>
    [HttpPost("{empresaId:int}/coordinadores")]
    public async Task<IActionResult> AsignarCoordinadorAsync(int empresaId, AsignarCoordinadorDto datos)
    {
        if (!EsAdministradorPlataforma() && !User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        if (!EsAdministradorPlataforma() && !await _personaServicio.PuedeGestionarseDesdeEmpresaAsync(datos.Cedula ?? string.Empty, empresaId))
        {
            return Conflict(new { mensaje = ConductoresController.MensajeInvitarPrimero });
        }

        try
        {
            await _asignacionCoordinadorServicio.AsignarAsync(empresaId, datos, ObtenerUsuarioEjecutorId());
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Consulta los coordinadores (activos e inactivos) de la empresa.
    /// Accesible para el administrador de plataforma o para un coordinador
    /// de esa misma empresa.
    /// </summary>
    [HttpGet("{empresaId:int}/coordinadores")]
    public async Task<ActionResult<List<CoordinadorDto>>> ObtenerCoordinadoresAsync(int empresaId)
    {
        if (!EsAdministradorPlataforma() && !User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _asignacionCoordinadorServicio.ObtenerPorEmpresaAsync(empresaId));
    }

    /// <summary>
    /// Revoca un rol COORDINADOR de la empresa. Accesible para el
    /// administrador de plataforma o para un coordinador activo de esa misma
    /// empresa (misma autorización que la asignación, ver <c>plan.md</c> §8.4).
    /// </summary>
    [HttpDelete("{empresaId:int}/coordinadores/{usuarioRolId:int}")]
    public async Task<IActionResult> RevocarCoordinadorAsync(int empresaId, int usuarioRolId)
    {
        if (!EsAdministradorPlataforma() && !User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _asignacionCoordinadorServicio.RevocarAsync(empresaId, usuarioRolId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Reactiva un rol COORDINADOR previamente revocado. Misma autorización
    /// que la revocación.
    /// </summary>
    [HttpPost("{empresaId:int}/coordinadores/{usuarioRolId:int}/reactivar")]
    public async Task<IActionResult> ReactivarCoordinadorAsync(int empresaId, int usuarioRolId)
    {
        if (!EsAdministradorPlataforma() && !User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _asignacionCoordinadorServicio.ReactivarAsync(empresaId, usuarioRolId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    private bool EsAdministradorPlataforma()
        => User.Claims.Any(c => c.Type == GeneradorTokenJwt.ClaimRol && c.Value == Rol.ADMINISTRADOR_PLATAFORMA.ToString());

    private int ObtenerUsuarioEjecutorId()
    {
        var valor = User.Claims.FirstOrDefault(c => c.Type == GeneradorTokenJwt.ClaimUsuarioId)?.Value;
        return int.Parse(valor ?? throw new InvalidOperationException("El token no contiene el identificador del usuario."));
    }
}
