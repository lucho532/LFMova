using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LFMova.Api.Configuration;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Api.Controllers;

/// <summary>
/// Entrega el informe de asistencia de una ruta (quiénes asistieron y quiénes no) como Excel. No contiene
/// lógica de negocio: delega en <see cref="IInformeAsistenciaServicio"/> y verifica que quien lo pide sea
/// coordinador de la empresa.
/// </summary>
[ApiController]
[Route("api/empresas/{empresaId:int}/jornadas/{jornadaId:int}/servicios/{servicioId:int}/informe-asistencia")]
[Authorize]
public class InformeAsistenciaRutaController : ControllerBase
{
    private readonly IInformeAsistenciaServicio _informeServicio;

    /// <summary>Crea el controlador con su servicio.</summary>
    public InformeAsistenciaRutaController(IInformeAsistenciaServicio informeServicio)
    {
        _informeServicio = informeServicio;
    }

    /// <summary>
    /// Descarga el Excel con los pasajeros de la ruta que cumplen el filtro (<c>TODOS</c>,
    /// <c>ASISTIERON</c> o <c>NO_ASISTIERON</c>). Solo un coordinador de esa empresa puede hacerlo.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DescargarAsync(int empresaId, int jornadaId, int servicioId, [FromQuery] FiltroAsistenciaRuta filtro = FiltroAsistenciaRuta.TODOS)
    {
        if (!User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
        {
            return Forbid();
        }

        try
        {
            var archivo = await _informeServicio.GenerarAsync(empresaId, servicioId, filtro);
            return File(archivo.Contenido, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", archivo.NombreArchivo);
        }
        catch (InvalidOperationException excepcion)
        {
            return NotFound(new { mensaje = excepcion.Message });
        }
    }
}
