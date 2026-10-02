using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Application.Interfaces;
using LFMova.Application.Mappers;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.ServiciosPasajero;

/// <summary>
/// Resuelve las consultas sobre pasajeros de servicios: quiénes van en un servicio, los servicios de
/// un empleado y a qué usuario (empleado o conductor) corresponde un pasajero. Solo lee: no cambia
/// estados ni asignaciones.
/// </summary>
public class ConsultasServicioPasajero
{
    private readonly AccesoServicioPasajero _acceso;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public ConsultasServicioPasajero(
        AccesoServicioPasajero acceso,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IServicioRepositorio servicioRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio)
    {
        _acceso = acceso;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _servicioRepositorio = servicioRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
    }

    /// <summary>Devuelve los pasajeros de un servicio de la empresa, con los datos de cada empleado.</summary>
    public async Task<List<ServicioPasajeroDto>> ObtenerPorServicioAsync(int empresaId, int servicioId)
    {
        await _acceso.ObtenerServicioDeLaEmpresaOFallarAsync(empresaId, servicioId);

        var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicioId);
        var resultado = new List<ServicioPasajeroDto>();
        foreach (var pasajero in pasajeros)
        {
            var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(pasajero.EmpleadoId);
            resultado.Add(ServicioPasajeroMapper.AServicioPasajeroDto(pasajero, empleado));
        }

        return resultado;
    }

    /// <summary>Devuelve el usuario del empleado que es ese pasajero, o <c>null</c> si no es de la empresa.</summary>
    public async Task<int?> ObtenerUsuarioIdEmpleadoAsync(int empresaId, int servicioPasajeroId)
    {
        var servicioPasajero = await _servicioPasajeroRepositorio.ObtenerPorIdAsync(servicioPasajeroId);
        if (servicioPasajero is null)
        {
            return null;
        }

        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(servicioPasajero.ServicioId);
        if (servicio is null || servicio.Jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(servicio.Jornada, empresaId))
        {
            return null;
        }

        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(servicioPasajero.EmpleadoId);
        return empleado?.UsuarioId;
    }

    /// <summary>Devuelve el usuario del conductor que lleva a ese pasajero, o <c>null</c> si el servicio no tiene conductor o no es de la empresa.</summary>
    public async Task<int?> ObtenerUsuarioIdConductorAsync(int empresaId, int servicioPasajeroId)
    {
        var servicioPasajero = await _servicioPasajeroRepositorio.ObtenerPorIdAsync(servicioPasajeroId);
        if (servicioPasajero is null)
        {
            return null;
        }

        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(servicioPasajero.ServicioId);
        if (servicio?.UnidadOperativaId is null || servicio.Jornada is null ||
            !ReglasMultiempresa.JornadaPerteneceAEmpresa(servicio.Jornada, empresaId))
        {
            return null;
        }

        var unidadOperativa = await _unidadOperativaRepositorio.ObtenerPorIdAsync(servicio.UnidadOperativaId.Value);
        if (unidadOperativa is null)
        {
            return null;
        }

        var conductor = await _conductorRepositorio.ObtenerPorIdAsync(unidadOperativa.ConductorId);
        return conductor?.UsuarioId;
    }

    /// <summary>Devuelve los servicios visibles del empleado que corresponde al usuario, del más reciente al más antiguo.</summary>
    public async Task<List<ServicioDelEmpleadoDto>> ObtenerPorUsuarioEmpleadoAsync(int usuarioId)
    {
        var empleado = await _empleadoRepositorio.ObtenerPorUsuarioIdAsync(usuarioId);
        if (empleado is null)
        {
            throw new InvalidOperationException("El usuario autenticado no tiene un perfil de empleado.");
        }

        var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorEmpleadoAsync(empleado.EmpleadoId);

        return pasajeros
            .Where(p => ReglasEstadoServicio.EsVisibleParaConductorYEmpleado(p.Servicio!.Estado))
            .OrderByDescending(p => p.Servicio!.Fecha)
            .ThenByDescending(p => p.Servicio!.HoraProgramada)
            .Select(ServicioPasajeroMapper.AServicioDelEmpleadoDto)
            .ToList();
    }
}
