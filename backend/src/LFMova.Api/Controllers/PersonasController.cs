using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.Personas;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Autenticacion;

namespace LFMova.Api.Controllers;

/// <summary>
/// Permite a un administrador de plataforma o a un coordinador buscar a una
/// persona registrada por su cédula, solo para verificar su identidad antes
/// de asignarle un rol. No contiene lógica de negocio: delega en
/// <see cref="IPersonaServicio"/>.
/// </summary>
[ApiController]
[Route("api/personas")]
[Authorize]
public class PersonasController : ControllerBase
{
    private readonly IPersonaServicio _personaServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public PersonasController(IPersonaServicio personaServicio)
    {
        _personaServicio = personaServicio;
    }

    /// <summary>
    /// Busca una persona por cédula (<c>?cedula=</c>). Solo administradores y
    /// coordinadores. Un coordinador solo encuentra a personas que ya forman
    /// parte de su empresa: a cualquier otra primero debe invitarla por correo.
    /// </summary>
    [HttpGet("buscar")]
    public async Task<ActionResult<PersonaDto>> BuscarAsync([FromQuery] string cedula)
    {
        if (!PuedeBuscarPersonas())
        {
            return Forbid();
        }

        try
        {
            var empresaRestringida = User.TieneRolGlobal(Rol.ADMINISTRADOR_PLATAFORMA) ? (int?)null : ObtenerEmpresaCoordinada();
            return Ok(await _personaServicio.BuscarPorCedulaAsync(cedula, empresaRestringida));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Empresa que coordina el usuario autenticado (un coordinador coordina una sola empresa).</summary>
    private int ObtenerEmpresaCoordinada()
    {
        var prefijo = $"{Rol.COORDINADOR}:";
        var claim = User.Claims.First(c => c.Type == GeneradorTokenJwt.ClaimRol && c.Value.StartsWith(prefijo));
        return int.Parse(claim.Value[prefijo.Length..]);
    }

    private bool PuedeBuscarPersonas()
        => User.Claims.Any(c =>
            c.Type == GeneradorTokenJwt.ClaimRol &&
            (c.Value == Rol.ADMINISTRADOR_PLATAFORMA.ToString() || c.Value.StartsWith($"{Rol.COORDINADOR}:")));
}
