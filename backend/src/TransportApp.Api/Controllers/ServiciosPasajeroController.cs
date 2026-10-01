using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportApp.Api.Configuration;
using TransportApp.Application.DTOs.Chat;
using TransportApp.Application.DTOs.ServiciosPasajero;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;

namespace TransportApp.Api.Controllers;

/// <summary>
/// Expone la asignación de programaciones a un servicio y la gestión de la
/// participación de sus pasajeros. No contiene lógica de negocio: delega en
/// <see cref="IServicioPasajeroServicio"/> y verifica el contexto de empresa
/// antes de invocarlo. Todas las operaciones están limitadas a un
/// coordinador de la empresa del servicio.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/jornadas/{jornadaId:int}/servicios/{servicioId:int}/pasajeros")]
[Authorize]
public class ServiciosPasajeroController : ControllerBase
{
    private readonly IServicioPasajeroServicio _servicioPasajeroServicio;
    private readonly IServicioServicio _servicioServicio;
    private readonly IChatServicio _chatServicio;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ServiciosPasajeroController(IServicioPasajeroServicio servicioPasajeroServicio, IServicioServicio servicioServicio, IChatServicio chatServicio)
    {
        _servicioPasajeroServicio = servicioPasajeroServicio;
        _servicioServicio = servicioServicio;
        _chatServicio = chatServicio;
    }

    /// <summary>Asigna una programación al servicio, creando su pasajero.</summary>
    [HttpPost]
    public async Task<ActionResult<ServicioPasajeroDto>> CrearAsync(int empresaId, int jornadaId, int servicioId, CrearServicioPasajeroDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            var pasajero = await _servicioPasajeroServicio.CrearAsync(empresaId, servicioId, datos);
            return CreatedAtAction(nameof(ObtenerPorServicioAsync), new { empresaId, jornadaId, servicioId }, pasajero);
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Consulta los pasajeros del servicio, incluyendo nombre y teléfono de
    /// contacto de cada empleado (ver <c>tasks.md</c> T086). Accesible para
    /// un coordinador de la empresa o para el conductor de la unidad
    /// operativa asignada al servicio, que la usa para ejecutar la ruta.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ServicioPasajeroDto>>> ObtenerPorServicioAsync(int empresaId, int jornadaId, int servicioId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            var usuarioIdConductor = await _servicioServicio.ObtenerUsuarioIdConductorAsignadoAsync(empresaId, servicioId);
            if (usuarioIdConductor is null || !User.EsUsuario(usuarioIdConductor.Value))
            {
                return Forbid();
            }
        }

        try
        {
            return Ok(await _servicioPasajeroServicio.ObtenerPorServicioAsync(empresaId, servicioId));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>El coordinador cancela la participación del pasajero en la ruta (queda con estado Cancelado, visible en el historial de la ruta).</summary>
    [HttpPost("{servicioPasajeroId:int}/cancelar")]
    public async Task<IActionResult> CancelarAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.CancelarAsync(empresaId, servicioPasajeroId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>El coordinador elimina por completo al pasajero de la ruta (para cuando se agregó por error); no queda registro de que estuvo ahí.</summary>
    [HttpDelete("{servicioPasajeroId:int}")]
    public async Task<IActionResult> EliminarAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.EliminarAsync(empresaId, servicioPasajeroId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>El coordinador corrige la dirección de recogida de este servicio puntual (por ejemplo, un dato mal pegado al crear la ruta).</summary>
    [HttpPut("{servicioPasajeroId:int}/direccion")]
    public async Task<IActionResult> EditarDireccionAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, EditarDireccionServicioPasajeroDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.EditarDireccionAsync(empresaId, servicioPasajeroId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>El coordinador mueve al pasajero a la ruta de otra unidad operativa (misma sede, fecha, hora y tipo); si esa ruta no existe, se crea.</summary>
    [HttpPut("{servicioPasajeroId:int}/reasignar")]
    public async Task<IActionResult> ReasignarAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, ReasignarServicioPasajeroDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.ReasignarAsync(empresaId, servicioPasajeroId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>El coordinador arrastra al pasajero a otra ruta ya existente de la misma sede, fecha, hora y tipo.</summary>
    [HttpPut("{servicioPasajeroId:int}/mover")]
    public async Task<IActionResult> MoverAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, MoverServicioPasajeroDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.MoverAsync(empresaId, servicioPasajeroId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Confirma la asistencia del pasajero, opcionalmente actualizando su dirección de recogida. Lo puede hacer un coordinador o el propio empleado.</summary>
    [HttpPost("{servicioPasajeroId:int}/confirmar")]
    public async Task<IActionResult> ConfirmarAsync(int empresaId, int servicioId, int servicioPasajeroId, ConfirmarServicioPasajeroDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId) && !await EsElPropioEmpleadoAsync(empresaId, servicioPasajeroId))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.ConfirmarAsync(empresaId, servicioPasajeroId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Marca que el pasajero no asistirá: notifica al conductor responsable
    /// del servicio.
    /// </summary>
    [HttpPost("{servicioPasajeroId:int}/no-asistira")]
    public async Task<IActionResult> MarcarNoAsistiraAsync(int empresaId, int servicioId, int servicioPasajeroId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId) && !await EsElPropioEmpleadoAsync(empresaId, servicioPasajeroId))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.MarcarNoAsistiraAsync(empresaId, servicioPasajeroId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Modifica el orden operativo del pasajero dentro del servicio. Lo puede hacer un coordinador de la empresa o el conductor asignado.</summary>
    [HttpPut("{servicioPasajeroId:int}/orden")]
    public async Task<IActionResult> ReordenarAsync(int empresaId, int servicioId, int servicioPasajeroId, ReordenarServicioPasajeroDto datos)
    {
        var usuarioIdConductor = await _servicioPasajeroServicio.ObtenerUsuarioIdConductorAsync(empresaId, servicioPasajeroId);
        var esConductorAsignado = usuarioIdConductor is not null && User.EsUsuario(usuarioIdConductor.Value);
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId) && !esConductorAsignado)
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.ReordenarAsync(empresaId, servicioPasajeroId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Registra manualmente que el conductor llegó al punto de recogida del
    /// pasajero (ver <c>tasks.md</c> T084). Es la alternativa manual mientras
    /// la detección automática por geolocalización permanezca pendiente de
    /// definición. Solo el conductor de la unidad operativa asignada al
    /// servicio puede registrarla.
    /// </summary>
    [HttpPost("{servicioPasajeroId:int}/marcar-llegada")]
    public async Task<IActionResult> MarcarLlegadaAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId)
    {
        var usuarioIdConductor = await _servicioPasajeroServicio.ObtenerUsuarioIdConductorAsync(empresaId, servicioPasajeroId);
        if (usuarioIdConductor is null || !User.EsUsuario(usuarioIdConductor.Value))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.MarcarLlegadaAsync(empresaId, servicioPasajeroId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Registra el resultado del procesamiento del pasajero tras la espera,
    /// o su avance dentro del vehículo (ver <c>tasks.md</c> T085/T090). Solo
    /// el conductor de la unidad operativa asignada al servicio puede
    /// registrarlo.
    /// </summary>
    [HttpPut("{servicioPasajeroId:int}/estado")]
    public async Task<IActionResult> CambiarEstadoAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, CambiarEstadoServicioPasajeroDto datos)
    {
        var usuarioIdConductor = await _servicioPasajeroServicio.ObtenerUsuarioIdConductorAsync(empresaId, servicioPasajeroId);
        if (usuarioIdConductor is null || !User.EsUsuario(usuarioIdConductor.Value))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.CambiarEstadoAsync(empresaId, servicioPasajeroId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Actualiza la ubicación compartida por el empleado para este servicio
    /// (ver <c>tasks.md</c> T088). Solo el propio empleado dueño del pasajero
    /// puede compartir su ubicación.
    /// </summary>
    [HttpPut("{servicioPasajeroId:int}/ubicacion")]
    public async Task<IActionResult> CompartirUbicacionAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, CompartirUbicacionDto datos)
    {
        var usuarioIdEmpleado = await _servicioPasajeroServicio.ObtenerUsuarioIdEmpleadoAsync(empresaId, servicioPasajeroId);
        if (usuarioIdEmpleado is null || !User.EsUsuario(usuarioIdEmpleado.Value))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.CompartirUbicacionAsync(empresaId, servicioPasajeroId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Guarda el punto GPS exacto de la recogida y lo conserva en el historial
    /// del empleado. Solo el conductor asignado al servicio puede hacerlo.
    /// </summary>
    [HttpPut("{servicioPasajeroId:int}/ubicacion-recogida")]
    public async Task<IActionResult> GuardarUbicacionRecogidaAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, CompartirUbicacionDto datos)
    {
        var usuarioIdConductor = await _servicioPasajeroServicio.ObtenerUsuarioIdConductorAsync(empresaId, servicioPasajeroId);
        if (usuarioIdConductor is null || !User.EsUsuario(usuarioIdConductor.Value))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.GuardarUbicacionRecogidaAsync(empresaId, servicioPasajeroId, datos);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Consulta las ubicaciones de recogida anteriores del empleado del
    /// pasajero, para poder navegar a ellas. Solo el conductor asignado o un
    /// coordinador de la empresa.
    /// </summary>
    [HttpGet("{servicioPasajeroId:int}/ubicaciones-anteriores")]
    public async Task<ActionResult<List<UbicacionAnteriorDto>>> ObtenerUbicacionesAnterioresAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId)
    {
        var usuarioIdConductor = await _servicioPasajeroServicio.ObtenerUsuarioIdConductorAsync(empresaId, servicioPasajeroId);
        var esConductorAsignado = usuarioIdConductor is not null && User.EsUsuario(usuarioIdConductor.Value);
        if (!esConductorAsignado && !User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _servicioPasajeroServicio.ObtenerUbicacionesAnterioresAsync(empresaId, servicioPasajeroId));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Olvida la ubicación guardada del empleado del pasajero, para que la
    /// próxima recogida guarde una nueva. Solo el conductor asignado o un
    /// coordinador de la empresa.
    /// </summary>
    [HttpDelete("{servicioPasajeroId:int}/ubicaciones-anteriores")]
    public async Task<IActionResult> EliminarUbicacionGuardadaAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId)
    {
        var usuarioIdConductor = await _servicioPasajeroServicio.ObtenerUsuarioIdConductorAsync(empresaId, servicioPasajeroId);
        var esConductorAsignado = usuarioIdConductor is not null && User.EsUsuario(usuarioIdConductor.Value);
        if (!esConductorAsignado && !User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicioPasajeroServicio.EliminarUbicacionGuardadaAsync(empresaId, servicioPasajeroId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Consulta los mensajes de la conversación individual del pasajero (ver
    /// <c>tasks.md</c> T087). Solo el conductor y el empleado involucrados en
    /// ese transporte pueden acceder.
    /// </summary>
    [HttpGet("{servicioPasajeroId:int}/mensajes")]
    public async Task<ActionResult<List<MensajeDto>>> ObtenerMensajesAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId)
    {
        try
        {
            if (!await EsParticipanteDeLaConversacionAsync(empresaId, servicioPasajeroId))
            {
                return Forbid();
            }

            return Ok(await _chatServicio.ObtenerMensajesAsync(empresaId, servicioPasajeroId));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Envía un mensaje en la conversación individual del pasajero (ver
    /// <c>tasks.md</c> T087). Solo el conductor y el empleado involucrados en
    /// ese transporte pueden enviar mensajes. No implementa chat grupal.
    /// </summary>
    [HttpPost("{servicioPasajeroId:int}/mensajes")]
    public async Task<ActionResult<MensajeDto>> EnviarMensajeAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, EnviarMensajeDto datos)
    {
        try
        {
            if (!await EsParticipanteDeLaConversacionAsync(empresaId, servicioPasajeroId))
            {
                return Forbid();
            }

            var mensaje = await _chatServicio.EnviarMensajeAsync(empresaId, servicioPasajeroId, User.ObtenerUsuarioId(), datos);
            return Ok(mensaje);
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    private async Task<bool> EsElPropioEmpleadoAsync(int empresaId, int servicioPasajeroId)
    {
        var usuarioIdEmpleado = await _servicioPasajeroServicio.ObtenerUsuarioIdEmpleadoAsync(empresaId, servicioPasajeroId);
        return usuarioIdEmpleado is not null && User.EsUsuario(usuarioIdEmpleado.Value);
    }

    private async Task<bool> EsParticipanteDeLaConversacionAsync(int empresaId, int servicioPasajeroId)
    {
        var (usuarioIdEmpleado, usuarioIdConductor) = await _chatServicio.ObtenerParticipantesAsync(empresaId, servicioPasajeroId);
        return (usuarioIdEmpleado is not null && User.EsUsuario(usuarioIdEmpleado.Value))
            || (usuarioIdConductor is not null && User.EsUsuario(usuarioIdConductor.Value));
    }
}
