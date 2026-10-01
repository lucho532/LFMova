using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.Conductores;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone la administración de conductores dentro del contexto de una
/// empresa: registro, vinculación y consulta. No contiene lógica de negocio:
/// delega en <see cref="IConductorServicio"/> y verifica el contexto de
/// empresa antes de invocarlo.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/conductores")]
[Authorize]
public class ConductoresController : ControllerBase
{
    /// <summary>Mensaje cuando un coordinador intenta asignar un rol a alguien que todavía no es parte de su empresa.</summary>
    public const string MensajeInvitarPrimero = "Esta persona todavía no hace parte de tu empresa: invítala primero por correo desde Empleados.";

    private readonly IConductorServicio _conductorServicio;
    private readonly IPersonaServicio _personaServicio;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ConductoresController(IConductorServicio conductorServicio, IPersonaServicio personaServicio)
    {
        _conductorServicio = conductorServicio;
        _personaServicio = personaServicio;
    }

    /// <summary>
    /// Registra un nuevo conductor y lo vincula con la empresa. Solo un
    /// coordinador de esa empresa o el administrador de plataforma puede hacerlo.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ConductorDto>> CrearAsync(int empresaId, CrearConductorDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId) && !User.TieneRolGlobal(Rol.ADMINISTRADOR_PLATAFORMA))
        {
            return Forbid();
        }

        if (!User.TieneRolGlobal(Rol.ADMINISTRADOR_PLATAFORMA) && !await _personaServicio.PuedeGestionarseDesdeEmpresaAsync(datos.Cedula ?? string.Empty, empresaId))
        {
            return Conflict(new { mensaje = MensajeInvitarPrimero });
        }

        try
        {
            var conductor = await _conductorServicio.CrearAsync(empresaId, datos);
            return CreatedAtAction(nameof(ObtenerPorIdAsync), new { empresaId, conductorId = conductor.ConductorId }, conductor);
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Vincula con la empresa un conductor ya registrado en la plataforma,
    /// sin duplicar su registro. Solo un coordinador de esa empresa puede
    /// hacerlo.
    /// </summary>
    [HttpPost("vincular")]
    public async Task<ActionResult<VinculacionConductorEmpresaDto>> VincularAsync(int empresaId, VincularConductorDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId) && !User.TieneRolGlobal(Rol.ADMINISTRADOR_PLATAFORMA))
        {
            return Forbid();
        }

        if (!User.TieneRolGlobal(Rol.ADMINISTRADOR_PLATAFORMA) && !await _personaServicio.PuedeGestionarseDesdeEmpresaAsync(datos.Cedula ?? string.Empty, empresaId))
        {
            return Conflict(new { mensaje = MensajeInvitarPrimero });
        }

        try
        {
            return Ok(await _conductorServicio.VincularAsync(empresaId, datos));
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Consulta los conductores vinculados a la empresa. Solo un coordinador
    /// de esa empresa puede hacerlo. No expone con qué otras empresas está
    /// vinculado cada conductor (ver <c>tasks.md</c> T097).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ConductorDto>>> ObtenerPorEmpresaAsync(int empresaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        var conductores = await _conductorServicio.ObtenerPorEmpresaAsync(empresaId);
        conductores.ForEach(OcultarEmpresasNoPropias);
        return Ok(conductores);
    }

    /// <summary>
    /// Consulta un conductor concreto vinculado a la empresa. Solo un
    /// coordinador de esa empresa puede hacerlo. No expone con qué otras
    /// empresas está vinculado el conductor (ver <c>tasks.md</c> T097): un
    /// conductor compartido entre varias empresas no debe revelar, a un
    /// coordinador de una de ellas, con cuáles otras trabaja.
    /// </summary>
    [HttpGet("{conductorId:int}")]
    public async Task<ActionResult<ConductorDto>> ObtenerPorIdAsync(int empresaId, int conductorId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        var conductor = await _conductorServicio.ObtenerPorIdAsync(conductorId);
        if (conductor is null || !conductor.EmpresaIdsVinculadosActivos.Contains(empresaId))
        {
            return NotFound();
        }

        OcultarEmpresasNoPropias(conductor);
        return Ok(conductor);
    }

    /// <summary>
    /// Restringe <see cref="ConductorDto.EmpresaIdsVinculadosActivos"/> a las
    /// empresas donde el usuario autenticado también es coordinador: estos
    /// endpoints solo se alcanzan como coordinador, nunca como el propio
    /// conductor, así que jamás deben revelar vinculaciones con empresas
    /// ajenas al coordinador que consulta (ver <c>tasks.md</c> T097).
    /// </summary>
    private void OcultarEmpresasNoPropias(ConductorDto conductor)
        => conductor.EmpresaIdsVinculadosActivos = conductor.EmpresaIdsVinculadosActivos
            .Where(id => User.TieneRolEnEmpresa(Rol.COORDINADOR, id))
            .ToList();

    /// <summary>
    /// Activa la vinculación de un conductor con la empresa. Solo un
    /// coordinador de esa empresa puede hacerlo.
    /// </summary>
    [HttpPost("{conductorId:int}/activar-vinculacion")]
    public async Task<IActionResult> ActivarVinculacionAsync(int empresaId, int conductorId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _conductorServicio.ActivarVinculacionAsync(empresaId, conductorId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Desactiva la vinculación de un conductor con la empresa. Solo un
    /// coordinador de esa empresa puede hacerlo.
    /// </summary>
    [HttpPost("{conductorId:int}/desactivar-vinculacion")]
    public async Task<IActionResult> DesactivarVinculacionAsync(int empresaId, int conductorId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _conductorServicio.DesactivarVinculacionAsync(empresaId, conductorId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }
}
