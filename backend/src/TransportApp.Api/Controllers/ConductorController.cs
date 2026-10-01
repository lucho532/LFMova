using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportApp.Api.Configuration;
using TransportApp.Application.DTOs.Conductores;
using TransportApp.Application.DTOs.Servicios;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;

namespace TransportApp.Api.Controllers;

/// <summary>
/// Expone la consulta y activación/desactivación global de un conductor,
/// fuera del contexto de una empresa concreta (un conductor puede estar
/// vinculado a varias). Accesible por el propio conductor autenticado o por
/// un coordinador de alguna empresa con la que el conductor tenga una
/// vinculación activa. No contiene lógica de negocio: delega en
/// <see cref="IConductorServicio"/>.
/// </summary>
[ApiController]
[Route("api/conductores")]
[Authorize]
public class ConductorController : ControllerBase
{
    private readonly IConductorServicio _conductorServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public ConductorController(IConductorServicio conductorServicio)
    {
        _conductorServicio = conductorServicio;
    }

    /// <summary>
    /// Consulta los servicios asignados al conductor autenticado (en
    /// cualquier empresa con la que tenga vinculación), más recientes
    /// primero. Se resuelve el conductor a partir del usuario del token, sin
    /// necesidad de conocer de antemano su <c>conductorId</c>.
    /// </summary>
    [HttpGet("mis-servicios")]
    public async Task<ActionResult<List<ServicioDto>>> ObtenerMisServiciosAsync()
    {
        try
        {
            return Ok(await _conductorServicio.ObtenerServiciosPropiosAsync(User.ObtenerUsuarioId()));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Consulta las unidades de trabajo del conductor autenticado, cada una
    /// con los datos completos de su vehículo.
    /// </summary>
    [HttpGet("mis-unidades")]
    public async Task<ActionResult<List<UnidadDeTrabajoDto>>> ObtenerMisUnidadesAsync()
    {
        try
        {
            return Ok(await _conductorServicio.ObtenerUnidadesPropiasAsync(User.ObtenerUsuarioId()));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }
}
