using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.Incidencias;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone el registro de incidencias y evidencias sobre un pasajero durante
/// la ejecución de un servicio (ver <c>tasks.md</c> T091/T092/T093). No
/// contiene lógica de negocio: delega en <see cref="IIncidenciaServicio"/>.
/// Solo el conductor de la unidad operativa asignada al servicio puede
/// registrarlas.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/jornadas/{jornadaId:int}/servicios/{servicioId:int}/pasajeros/{servicioPasajeroId:int}/incidencias")]
[Authorize]
public class IncidenciasController : ControllerBase
{
    private readonly IIncidenciaServicio _incidenciaServicio;
    private readonly IAlmacenamientoArchivos _almacenamiento;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public IncidenciasController(IIncidenciaServicio incidenciaServicio, IAlmacenamientoArchivos almacenamiento)
    {
        _incidenciaServicio = incidenciaServicio;
        _almacenamiento = almacenamiento;
    }

    /// <summary>Registra una nueva incidencia sobre el pasajero, incluyendo su ubicación cuando esté disponible.</summary>
    [HttpPost]
    public async Task<ActionResult<IncidenciaDto>> CrearAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, CrearIncidenciaDto datos)
    {
        if (!await EsConductorAsignadoAsync(empresaId, servicioPasajeroId))
        {
            return Forbid();
        }

        try
        {
            var incidencia = await _incidenciaServicio.CrearAsync(empresaId, servicioPasajeroId, datos);
            return CreatedAtAction(nameof(ObtenerPorPasajeroAsync), new { empresaId, jornadaId, servicioId, servicioPasajeroId }, incidencia);
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Consulta las incidencias del pasajero, con sus evidencias.</summary>
    [HttpGet]
    public async Task<ActionResult<List<IncidenciaDto>>> ObtenerPorPasajeroAsync(int empresaId, int jornadaId, int servicioId, int servicioPasajeroId)
    {
        if (!await EsConductorAsignadoAsync(empresaId, servicioPasajeroId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _incidenciaServicio.ObtenerPorServicioPasajeroAsync(empresaId, servicioPasajeroId));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Consulta las incidencias de todos los pasajeros del servicio, con sus evidencias, para el resumen
    /// de la ruta del coordinador. Solo un coordinador de la empresa puede usarla.
    /// </summary>
    [HttpGet("~/api/empresas/{empresaId:int}/jornadas/{jornadaId:int}/servicios/{servicioId:int}/incidencias")]
    public async Task<ActionResult<List<IncidenciaDto>>> ObtenerPorServicioAsync(int empresaId, int jornadaId, int servicioId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _incidenciaServicio.ObtenerPorServicioAsync(empresaId, jornadaId, servicioId));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Sube una fotografía (JPEG, PNG, WebP o HEIC, hasta 10 MB) y la asocia
    /// como evidencia de la incidencia. El archivo se guarda fuera de la base
    /// de datos; esta solo conserva su referencia.
    /// </summary>
    [HttpPost("{incidenciaId:int}/evidencias/foto")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<ActionResult<EvidenciaDto>> SubirFotoAsync(
        int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, int incidenciaId, IFormFile archivo)
    {
        if (!await EsConductorAsignadoAsync(empresaId, servicioPasajeroId))
        {
            return Forbid();
        }

        var extensiones = new Dictionary<string, string>
        {
            ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/webp"] = ".webp", ["image/heic"] = ".heic", ["image/heif"] = ".heic"
        };
        if (archivo is null || archivo.Length == 0 || archivo.Length > 10 * 1024 * 1024 || !extensiones.TryGetValue(archivo.ContentType.ToLowerInvariant(), out var extension))
        {
            return BadRequest(new { mensaje = "La foto debe ser una imagen JPEG, PNG, WebP o HEIC de hasta 10 MB." });
        }

        try
        {
            // Se valida que la incidencia exista y sea del pasajero antes de guardar el archivo.
            var incidencias = await _incidenciaServicio.ObtenerPorServicioPasajeroAsync(empresaId, servicioPasajeroId);
            if (incidencias.All(i => i.IncidenciaId != incidenciaId))
            {
                return NotFound(new { mensaje = "La incidencia indicada no existe para este pasajero." });
            }

            await using var contenido = archivo.OpenReadStream();
            var referencia = await _almacenamiento.GuardarAsync($"evidencias/{empresaId}", extension, contenido);
            return Ok(await _incidenciaServicio.AgregarEvidenciaAsync(
                empresaId, servicioPasajeroId, incidenciaId, new AgregarEvidenciaDto { Tipo = TipoEvidencia.FOTOGRAFIA, ReferenciaArchivo = referencia }));
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Descarga la fotografía de una evidencia. Solo el conductor asignado o un coordinador de la empresa.</summary>
    [HttpGet("{incidenciaId:int}/evidencias/{evidenciaId:int}/archivo")]
    public async Task<IActionResult> ObtenerArchivoAsync(
        int empresaId, int jornadaId, int servicioId, int servicioPasajeroId, int incidenciaId, int evidenciaId)
    {
        if (!await EsConductorAsignadoAsync(empresaId, servicioPasajeroId) && !User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        var incidencias = await _incidenciaServicio.ObtenerPorServicioPasajeroAsync(empresaId, servicioPasajeroId);
        var evidencia = incidencias.Where(i => i.IncidenciaId == incidenciaId).SelectMany(i => i.Evidencias).FirstOrDefault(e => e.EvidenciaId == evidenciaId);
        var flujo = evidencia is null ? null : await _almacenamiento.AbrirAsync(evidencia.ReferenciaArchivo);
        if (flujo is null)
        {
            return NotFound();
        }

        var tipos = new Dictionary<string, string> { [".jpg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp", [".heic"] = "image/heic" };
        return File(flujo, tipos.GetValueOrDefault(Path.GetExtension(evidencia!.ReferenciaArchivo), "application/octet-stream"));
    }

    private async Task<bool> EsConductorAsignadoAsync(int empresaId, int servicioPasajeroId)
    {
        var usuarioIdConductor = await _incidenciaServicio.ObtenerUsuarioIdConductorAsync(empresaId, servicioPasajeroId);
        return usuarioIdConductor is not null && User.EsUsuario(usuarioIdConductor.Value);
    }
}
