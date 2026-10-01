using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportApp.Api.Configuration;
using TransportApp.Application.DTOs.Zonas;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;
using TransportApp.Domain.Exceptions;
using TransportApp.Infrastructure.Autenticacion;

namespace TransportApp.Api.Controllers;

/// <summary>
/// Expone la reorganización de las zonas registradas de una empresa: mover un
/// barrio a otra zona, unir dos zonas y eliminar una zona. No contiene lógica
/// de negocio: delega en <see cref="IZonaReorganizacionServicio"/> y verifica
/// el contexto de empresa antes de invocarlo.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/zonas/{zonaId:int}")]
[Authorize]
public class ZonasReorganizacionController : ControllerBase
{
    private readonly IZonaReorganizacionServicio _reorganizacionServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public ZonasReorganizacionController(IZonaReorganizacionServicio reorganizacionServicio)
    {
        _reorganizacionServicio = reorganizacionServicio;
    }

    /// <summary>Pasa un barrio de esta zona a otra. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost("mover-barrio")]
    public Task<IActionResult> MoverBarrioAsync(int empresaId, int zonaId, MoverBarrioZonaDto datos)
        => EjecutarAsync(empresaId, () => _reorganizacionServicio.MoverBarrioAsync(empresaId, zonaId, datos.Barrio, datos.ZonaDestinoId));

    /// <summary>Une esta zona con otra, que recibe todos sus barrios. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpPost("unir")]
    public Task<IActionResult> UnirAsync(int empresaId, int zonaId, UnirZonaDto datos)
        => EjecutarAsync(empresaId, () => _reorganizacionServicio.UnirAsync(empresaId, zonaId, datos.ZonaDestinoId));

    /// <summary>Elimina esta zona. Solo un coordinador de esa empresa puede hacerlo.</summary>
    [HttpDelete]
    public Task<IActionResult> EliminarAsync(int empresaId, int zonaId)
        => EjecutarAsync(empresaId, () => _reorganizacionServicio.EliminarAsync(empresaId, zonaId));

    /// <summary>Verifica el rol en la empresa, ejecuta la operación y traduce sus errores a respuestas HTTP.</summary>
    private async Task<IActionResult> EjecutarAsync(int empresaId, Func<Task> operacion)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await operacion();
            return NoContent();
        }
        catch (BarreraGeograficaConflictoException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }
}
