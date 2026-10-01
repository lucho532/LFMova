using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportApp.Api.Configuration;
using TransportApp.Application.DTOs.Estadisticas;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;

namespace TransportApp.Api.Controllers;

/// <summary>
/// Expone las consultas de actividad (rutas y pasajeros) de una empresa para
/// su coordinador: resumen del inicio y actividad por conductor. Solo lectura;
/// no contiene lógica de negocio.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/estadisticas")]
[Authorize]
public class EstadisticasController : ControllerBase
{
    private readonly IEstadisticasRepositorio _estadisticas;

    /// <summary>Crea el controlador con su fuente de datos.</summary>
    public EstadisticasController(IEstadisticasRepositorio estadisticas)
    {
        _estadisticas = estadisticas;
    }

    /// <summary>Resumen histórico de la empresa (rutas, pasajeros, actividad reciente).</summary>
    [HttpGet("resumen")]
    public async Task<ActionResult<ResumenEmpresaDto>> ObtenerResumenAsync(int empresaId)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _estadisticas.ObtenerResumenAsync(empresaId));
    }

    /// <summary>Actividad de cada conductor de la empresa, opcionalmente entre dos fechas.</summary>
    [HttpGet("conductores")]
    public async Task<ActionResult<List<EstadisticaConductorDto>>> ObtenerPorConductorAsync(int empresaId, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _estadisticas.ObtenerPorConductorAsync(empresaId, desde, hasta));
    }

    /// <summary>Rutas de un conductor, opcionalmente entre dos fechas.</summary>
    [HttpGet("conductores/{conductorId:int}/rutas")]
    public async Task<ActionResult<List<RutaConductorDto>>> ObtenerRutasAsync(int empresaId, int conductorId, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _estadisticas.ObtenerRutasDeConductorAsync(empresaId, conductorId, desde, hasta));
    }

    /// <summary>
    /// Rutas de la empresa con horario, unidad y conductor. <c>filtro</c>: programadas, realizadas, por-realizar,
    /// canceladas o vacío para todas.
    /// </summary>
    [HttpGet("rutas")]
    public async Task<ActionResult<List<RutaEmpresaDto>>> ObtenerRutasAsync(int empresaId, [FromQuery] string? filtro, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _estadisticas.ObtenerRutasAsync(empresaId, filtro, desde, hasta));
    }

    /// <summary>Pasajeros de las rutas de la empresa; <c>soloTransportados=true</c> limita a los ya dejados en su destino.</summary>
    [HttpGet("pasajeros")]
    public async Task<ActionResult<List<PasajeroEmpresaDto>>> ObtenerPasajerosAsync(int empresaId, [FromQuery] bool soloTransportados, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        return Ok(await _estadisticas.ObtenerPasajerosAsync(empresaId, soloTransportados, desde, hasta));
    }
}
