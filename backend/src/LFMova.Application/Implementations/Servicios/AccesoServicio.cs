using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.Servicios;

/// <summary>
/// Obtiene un servicio comprobando que su jornada pertenezca a la empresa indicada (ver
/// <c>AGENTS.md</c> §12: nunca se asume que un id recibido es de la empresa del usuario). No decide
/// permisos por rol ni modifica nada.
/// </summary>
public class AccesoServicio
{
    private readonly IServicioRepositorio _servicioRepositorio;

    /// <summary>Crea el colaborador con su repositorio.</summary>
    public AccesoServicio(IServicioRepositorio servicioRepositorio)
    {
        _servicioRepositorio = servicioRepositorio;
    }

    /// <summary>Devuelve el servicio si existe y es de la empresa; si no, <c>null</c>.</summary>
    public async Task<Servicio?> ObtenerDeLaEmpresaAsync(int empresaId, int servicioId)
    {
        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(servicioId);
        if (servicio is null || servicio.Jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(servicio.Jornada, empresaId))
        {
            return null;
        }

        return servicio;
    }
}
