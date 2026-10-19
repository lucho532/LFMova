using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.DTOs.UnidadesOperativas;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Api.Controllers;

/// <summary>
/// Expone el número interno que una empresa le asigna a las unidades operativas de sus conductores. Solo un
/// coordinador de la empresa puede consultarlo o cambiarlo. No contiene lógica de negocio: delega en
/// <see cref="INumeroInternoUnidadServicio"/>.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/unidades-operativas")]
[Authorize]
public class NumerosInternosUnidadController : ControllerBase
{
    private readonly INumeroInternoUnidadServicio _servicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public NumerosInternosUnidadController(INumeroInternoUnidadServicio servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Números internos que la empresa ha asignado a sus unidades.</summary>
    [HttpGet("numeros-internos")]
    public async Task<ActionResult<List<NumeroInternoUnidadDto>>> ObtenerAsync(int empresaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _servicio.ObtenerPorEmpresaAsync(empresaId));
    }

    /// <summary>Asigna, cambia o quita (con número vacío) el número interno de una unidad para la empresa.</summary>
    [HttpPut("{unidadOperativaId:int}/numero-interno")]
    public async Task<IActionResult> AsignarAsync(int empresaId, int unidadOperativaId, [FromBody] AsignarNumeroInternoDto datos)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            await _servicio.AsignarAsync(empresaId, unidadOperativaId, datos.Numero);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }
}
