using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.ServiciosPasajero;

/// <summary>
/// Obtiene un servicio o un pasajero comprobando que pertenezcan a la empresa indicada (ver
/// <c>AGENTS.md</c> §12: nunca se asume que un id recibido es de la empresa del usuario). No decide
/// permisos por rol ni modifica nada.
/// </summary>
public class AccesoServicioPasajero
{
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IServicioRepositorio _servicioRepositorio;

    /// <summary>Crea el colaborador con sus repositorios.</summary>
    public AccesoServicioPasajero(IServicioPasajeroRepositorio servicioPasajeroRepositorio, IServicioRepositorio servicioRepositorio)
    {
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _servicioRepositorio = servicioRepositorio;
    }

    /// <summary>Devuelve el servicio si existe y su jornada es de la empresa; si no, falla.</summary>
    public async Task<Servicio> ObtenerServicioDeLaEmpresaOFallarAsync(int empresaId, int servicioId)
    {
        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(servicioId);
        if (servicio is null || servicio.Jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(servicio.Jornada, empresaId))
        {
            throw new InvalidOperationException("El servicio indicado no existe en esta empresa.");
        }

        return servicio;
    }

    /// <summary>Devuelve el pasajero si existe y su servicio es de la empresa; si no, falla.</summary>
    public async Task<ServicioPasajero> ObtenerPasajeroDeLaEmpresaOFallarAsync(int empresaId, int servicioPasajeroId)
    {
        var servicioPasajero = await _servicioPasajeroRepositorio.ObtenerPorIdAsync(servicioPasajeroId);
        if (servicioPasajero is null)
        {
            throw new InvalidOperationException("El pasajero indicado no existe.");
        }

        await ObtenerServicioDeLaEmpresaOFallarAsync(empresaId, servicioPasajero.ServicioId);
        return servicioPasajero;
    }
}
