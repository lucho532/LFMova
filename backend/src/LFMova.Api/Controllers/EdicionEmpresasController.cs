using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Application.DTOs.Empresas;
using LFMova.Application.Interfaces;

namespace LFMova.Api.Controllers;

/// <summary>
/// Corrección y eliminación de empresas ya creadas, solo para el
/// administrador de plataforma. Comparte la ruta con
/// <see cref="EmpresasController"/>, del que se separa para no superar el
/// tamaño máximo de archivo. No contiene lógica de negocio: delega en
/// <see cref="IEdicionEmpresaServicio"/>.
/// </summary>
[ApiController]
[Route("api/empresas")]
[Authorize(Policy = "RequiereAdministradorPlataforma")]
public class EdicionEmpresasController : ControllerBase
{
    private readonly IEdicionEmpresaServicio _edicionServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public EdicionEmpresasController(IEdicionEmpresaServicio edicionServicio)
    {
        _edicionServicio = edicionServicio;
    }

    /// <summary>Corrige el nombre, el CIF y la dirección de una empresa.</summary>
    [HttpPut("{empresaId:int}")]
    public async Task<ActionResult<EmpresaDto>> ActualizarAsync(int empresaId, ActualizarEmpresaDto datos)
    {
        try
        {
            return Ok(await _edicionServicio.ActualizarAsync(empresaId, datos));
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }

    /// <summary>
    /// Elimina una empresa por completo, con todo su historial. Las personas
    /// conservan su cuenta, ya sin empresa.
    /// </summary>
    [HttpDelete("{empresaId:int}")]
    public async Task<IActionResult> EliminarAsync(int empresaId)
    {
        try
        {
            await _edicionServicio.EliminarAsync(empresaId);
            return NoContent();
        }
        catch (InvalidOperationException excepcion)
        {
            return Conflict(new { mensaje = excepcion.Message });
        }
    }
}
