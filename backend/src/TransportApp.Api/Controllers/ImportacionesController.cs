using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportApp.Api.Configuration;
using TransportApp.Application.DTOs.Importaciones;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;

namespace TransportApp.Api.Controllers;

/// <summary>
/// Expone la importación de la programación diaria de un conductor desde un
/// Excel: validar (sin guardar) e importar. Solo un coordinador de la empresa
/// puede usarla. No contiene lógica de negocio: delega en
/// <see cref="IImportacionExcelServicio"/>.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/importaciones")]
[Authorize]
public class ImportacionesController : ControllerBase
{
    private readonly IImportacionExcelServicio _importacionServicio;
    private readonly IPlantillaColumnasPegadoServicio _plantillaColumnasPegadoServicio;

    /// <summary>Crea el controlador con sus servicios.</summary>
    public ImportacionesController(IImportacionExcelServicio importacionServicio, IPlantillaColumnasPegadoServicio plantillaColumnasPegadoServicio)
    {
        _importacionServicio = importacionServicio;
        _plantillaColumnasPegadoServicio = plantillaColumnasPegadoServicio;
    }

    /// <summary>Interpreta el archivo y devuelve qué contiene y qué pasaría al importarlo. No guarda nada.</summary>
    [HttpPost("validar")]
    public async Task<ActionResult<VistaPreviaImportacionDto>> ValidarAsync(int empresaId, IFormFile archivo)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await using var flujo = await CopiarAsync(archivo);
            return Ok(await _importacionServicio.ValidarAsync(empresaId, flujo));
        }
        catch (InvalidOperationException excepcion)
        {
            return BadRequest(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Importa el archivo en la jornada de la fecha operativa indicada. Si se
    /// indica <c>unidadOperativaId</c>, se asigna a todos los servicios.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ResultadoImportacionDto>> ImportarAsync(
        int empresaId, IFormFile archivo, [FromForm] DateOnly fechaOperativa, [FromForm] int? unidadOperativaId, [FromForm] bool repartirEntreUnidades = false,
        [FromForm] int? sedeGeneralId = null)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await using var flujo = await CopiarAsync(archivo);
            return Ok(await _importacionServicio.ImportarAsync(
                empresaId, User.ObtenerUsuarioId(), archivo.FileName, flujo, fechaOperativa, unidadOperativaId, repartirEntreUnidades, sedeGeneralId));
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Agrega a mano a un empleado que no venía en el Excel y lo asigna a una ruta ya creada.</summary>
    [HttpPost("empleado-manual")]
    public async Task<ActionResult<ResultadoImportacionDto>> AgregarEmpleadoManualAsync(int empresaId, [FromBody] AgregarEmpleadoManualDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _importacionServicio.AgregarEmpleadoARutaAsync(empresaId, datos));
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Crea una ruta a mano para una unidad operativa (o agrega el pasajero a
    /// la que ya exista con esa fecha, hora, tipo, sede y unidad).
    /// </summary>
    [HttpPost("ruta-manual")]
    public async Task<ActionResult<ResultadoImportacionDto>> CrearRutaManualAsync(int empresaId, [FromBody] CrearRutaManualDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _importacionServicio.CrearRutaManualAsync(empresaId, datos));
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Crea una ruta vacía a mano para una unidad operativa (o reutiliza la
    /// que ya exista con esa misma fecha, hora, tipo, sede y unidad), sin
    /// pasajeros: sirve para abrir un destino nuevo y luego moverle
    /// pasajeros desde otra ruta.
    /// </summary>
    [HttpPost("ruta-vacia")]
    public async Task<ActionResult<ResultadoImportacionDto>> CrearRutaVaciaAsync(int empresaId, [FromBody] CrearRutaVaciaDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _importacionServicio.CrearRutaVaciaAsync(empresaId, datos));
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Crea una ruta nueva a partir de pasajeros pegados desde un Excel
    /// externo, sin conductor asignado.
    /// </summary>
    [HttpPost("ruta-pegada")]
    public async Task<ActionResult<ResultadoImportacionDto>> CrearRutaPegadaAsync(int empresaId, [FromBody] CrearRutaPegadaDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _importacionServicio.CrearRutaPegadaAsync(empresaId, datos));
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>Consulta la plantilla de orden de columnas de la empresa, o 404 si todavía no definió ninguna.</summary>
    [HttpGet("plantilla-columnas-pegado")]
    public async Task<ActionResult<PlantillaColumnasPegadoDto>> ObtenerPlantillaColumnasPegadoAsync(int empresaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        var plantilla = await _plantillaColumnasPegadoServicio.ObtenerPorEmpresaAsync(empresaId);
        return plantilla is null ? NotFound() : Ok(plantilla);
    }

    /// <summary>
    /// Guarda (crea o reemplaza) la plantilla de orden de columnas de la
    /// empresa.
    /// </summary>
    [HttpPut("plantilla-columnas-pegado")]
    public async Task<ActionResult<PlantillaColumnasPegadoDto>> GuardarPlantillaColumnasPegadoAsync(int empresaId, [FromBody] GuardarPlantillaColumnasPegadoDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _plantillaColumnasPegadoServicio.GuardarAsync(empresaId, datos));
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    private static async Task<MemoryStream> CopiarAsync(IFormFile archivo)
    {
        var copia = new MemoryStream();
        await archivo.CopyToAsync(copia);
        copia.Position = 0;
        return copia;
    }
}
