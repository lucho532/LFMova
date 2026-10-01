using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportApp.Api.Configuration;
using TransportApp.Application.DTOs.Planificacion;
using TransportApp.Application.DTOs.Servicios;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;

namespace TransportApp.Api.Controllers;

/// <summary>
/// Expone la creación y gestión del ciclo de vida de servicios de una
/// jornada. No contiene lógica de negocio: delega en
/// <see cref="IServicioServicio"/> y verifica el contexto de empresa antes de
/// invocarlo.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/jornadas/{jornadaId:int}/servicios")]
[Authorize]
public class ServiciosController : ControllerBase
{
    private readonly IServicioServicio _servicioServicio;
    private readonly IPlanificacionServicio _planificacionServicio;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ServiciosController(IServicioServicio servicioServicio, IPlanificacionServicio planificacionServicio)
    {
        _servicioServicio = servicioServicio;
        _planificacionServicio = planificacionServicio;
    }

    /// <summary>Crea un servicio en la jornada. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost]
    public async Task<ActionResult<ServicioDto>> CrearAsync(int empresaId, int jornadaId, CrearServicioDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            var servicio = await _servicioServicio.CrearAsync(empresaId, jornadaId, datos);
            return CreatedAtAction(nameof(ObtenerPorIdAsync), new { empresaId, jornadaId, servicioId = servicio.ServicioId }, servicio);
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Consulta los servicios de la jornada. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ServicioDto>>> ObtenerPorJornadaAsync(int empresaId, int jornadaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _servicioServicio.ObtenerPorJornadaAsync(empresaId, jornadaId));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Consulta un servicio concreto de la jornada.</summary>
    [HttpGet("{servicioId:int}")]
    public async Task<ActionResult<ServicioDto>> ObtenerPorIdAsync(int empresaId, int jornadaId, int servicioId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        var servicio = await _servicioServicio.ObtenerPorIdAsync(empresaId, servicioId);
        return servicio is null || servicio.JornadaId != jornadaId ? NotFound() : Ok(servicio);
    }

    /// <summary>
    /// Cambia el estado del servicio. Solo un coordinador de esa empresa
    /// puede hacerlo.
    /// </summary>
    [HttpPost("{servicioId:int}/cambiar-estado")]
    public async Task<IActionResult> CambiarEstadoAsync(int empresaId, int jornadaId, int servicioId, CambiarEstadoServicioDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicioServicio.CambiarEstadoAsync(empresaId, servicioId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Asigna o reasigna la unidad operativa del servicio. La reasignación
    /// no modifica la jornada del servicio. Solo un coordinador de esa
    /// empresa puede hacerlo.
    /// </summary>
    [HttpPost("{servicioId:int}/asignar-unidad")]
    public async Task<IActionResult> AsignarUnidadAsync(int empresaId, int jornadaId, int servicioId, AsignarUnidadServicioDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicioServicio.AsignarUnidadAsync(empresaId, servicioId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Retira la unidad operativa del servicio, dejándolo disponible para
    /// reorganización. Solo un coordinador de esa empresa puede hacerlo.
    /// </summary>
    [HttpPost("{servicioId:int}/retirar-unidad")]
    public async Task<IActionResult> RetirarUnidadAsync(int empresaId, int jornadaId, int servicioId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicioServicio.RetirarUnidadAsync(empresaId, servicioId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// "Despublica" una ruta ya enviada para volver a poder editarla (por
    /// ejemplo, agregarle un pasajero de última hora). Si tiene conductor, se
    /// le notifica. Solo un coordinador de esa empresa puede hacerlo.
    /// </summary>
    [HttpPost("{servicioId:int}/despublicar")]
    public async Task<IActionResult> DespublicarAsync(int empresaId, int jornadaId, int servicioId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicioServicio.DespublicarAsync(empresaId, servicioId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Elimina por completo la ruta (borrado físico). Falla si está en curso
    /// o ya finalizada. Solo un coordinador de esa empresa puede hacerlo.
    /// </summary>
    [HttpDelete("{servicioId:int}")]
    public async Task<IActionResult> EliminarAsync(int empresaId, int jornadaId, int servicioId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicioServicio.EliminarAsync(empresaId, servicioId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Inicia manualmente la ejecución del servicio. Solo el conductor de la
    /// unidad operativa asignada al servicio puede iniciarlo (ver
    /// <c>spec.md</c> §24 y <c>tasks.md</c> T083).
    /// </summary>
    [HttpPost("{servicioId:int}/iniciar")]
    public async Task<IActionResult> IniciarAsync(int empresaId, int jornadaId, int servicioId, [FromBody] IniciarServicioDto? datos = null)
    {
        var usuarioIdConductor = await _servicioServicio.ObtenerUsuarioIdConductorAsignadoAsync(empresaId, servicioId);
        if (usuarioIdConductor is null || !User.EsUsuario(usuarioIdConductor.Value))
        {
            return Forbid();
        }

        try
        {
            await _servicioServicio.IniciarAsync(empresaId, servicioId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Finaliza manualmente la ejecución del servicio. Solo el conductor de
    /// la unidad operativa asignada al servicio puede finalizarlo (ver
    /// <c>spec.md</c> §27 y <c>tasks.md</c> T090).
    /// </summary>
    [HttpPost("{servicioId:int}/finalizar")]
    public async Task<IActionResult> FinalizarAsync(int empresaId, int jornadaId, int servicioId, [FromBody] FinalizarServicioDto? datos = null)
    {
        var usuarioIdConductor = await _servicioServicio.ObtenerUsuarioIdConductorAsignadoAsync(empresaId, servicioId);
        if (usuarioIdConductor is null || !User.EsUsuario(usuarioIdConductor.Value))
        {
            return Forbid();
        }

        try
        {
            await _servicioServicio.FinalizarAsync(empresaId, servicioId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Genera la propuesta de planificación asistida del servicio (ver
    /// <c>tasks.md</c> Fase 14): orden sugerido de pasajeros, horarios y
    /// continuidad geográfica. Es solo una recomendación de lectura; el
    /// coordinador puede aceptarla, modificarla o ignorarla mediante las
    /// operaciones manuales existentes (asignar unidad, reordenar
    /// pasajeros). Solo un coordinador de esa empresa puede consultarla.
    /// </summary>
    [HttpGet("{servicioId:int}/propuesta-planificacion")]
    public async Task<ActionResult<PropuestaPlanificacionDto>> ObtenerPropuestaPlanificacionAsync(int empresaId, int jornadaId, int servicioId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _planificacionServicio.GenerarPropuestaAsync(empresaId, servicioId));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }
}
