using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Application.Interfaces;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone la consulta de los propios servicios de transporte del empleado
/// autenticado, fuera del contexto de una empresa concreta en la URL: el
/// empleado se resuelve a partir del usuario del token, no de un
/// <c>empleadoId</c> recibido por separado. No contiene lógica de negocio:
/// delega en <see cref="IServicioPasajeroServicio"/>.
/// </summary>
[ApiController]
[Route("api/empleados")]
[Authorize]
public class EmpleadoController : ControllerBase
{
    private readonly IServicioPasajeroServicio _servicioPasajeroServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public EmpleadoController(IServicioPasajeroServicio servicioPasajeroServicio)
    {
        _servicioPasajeroServicio = servicioPasajeroServicio;
    }

    /// <summary>
    /// Consulta los servicios (como pasajero) del empleado autenticado, en
    /// cualquier empresa, más recientes primero.
    /// </summary>
    [HttpGet("mis-servicios")]
    public async Task<ActionResult<List<ServicioDelEmpleadoDto>>> ObtenerMisServiciosAsync()
    {
        try
        {
            return Ok(await _servicioPasajeroServicio.ObtenerPorUsuarioEmpleadoAsync(User.ObtenerUsuarioId()));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }
}
