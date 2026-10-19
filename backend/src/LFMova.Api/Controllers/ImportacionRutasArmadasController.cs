using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone la importación de un Excel cuyas rutas ya vienen armadas: validar (sin guardar) e importar. Solo un
/// coordinador de la empresa puede usarla. No contiene lógica de negocio: delega en
/// <see cref="IImportacionRutasArmadasServicio"/>.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/importaciones/rutas-armadas")]
[Authorize]
public class ImportacionRutasArmadasController : ControllerBase
{
    private readonly IImportacionRutasArmadasServicio _servicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public ImportacionRutasArmadasController(IImportacionRutasArmadasServicio servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Interpreta el archivo y devuelve las rutas que contiene y qué pasaría al importarlo. No guarda nada.</summary>
    [HttpPost("validar")]
    public async Task<ActionResult<VistaPreviaRutasArmadasDto>> ValidarAsync(
        int empresaId, IFormFile archivo, [FromForm] ModoSeparacionRutas modo, [FromForm] string? textoInicioRuta)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await using var flujo = await CopiarAsync(archivo);
            return Ok(await _servicio.ValidarAsync(empresaId, flujo, modo, textoInicioRuta));
        }
        catch (InvalidOperationException excepcion)
        {
            return BadRequest(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Crea una ruta por cada ruta del archivo, tal como viene y sin publicarlas.</summary>
    [HttpPost]
    public async Task<ActionResult<ResultadoImportacionDto>> ImportarAsync(
        int empresaId, IFormFile archivo, [FromForm] ModoSeparacionRutas modo, [FromForm] string? textoInicioRuta, [FromForm] DateOnly fechaOperativa, [FromForm] int? sedeGeneralId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await using var flujo = await CopiarAsync(archivo);
            return Ok(await _servicio.ImportarAsync(empresaId, User.ObtenerUsuarioId(), archivo.FileName, flujo, modo, textoInicioRuta, fechaOperativa, sedeGeneralId));
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    private static async Task<MemoryStream> CopiarAsync(IFormFile archivo)
    {
        var flujo = new MemoryStream();
        await archivo.CopyToAsync(flujo);
        flujo.Position = 0;
        return flujo;
    }
}
