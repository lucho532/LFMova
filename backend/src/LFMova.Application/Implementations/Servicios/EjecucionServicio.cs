using LFMova.Application.DTOs.Servicios;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.Servicios;

/// <summary>
/// Registra lo que hace el conductor al recorrer la ruta: iniciarla y finalizarla, con el instante
/// real en UTC y el punto GPS (ver <c>AGENTS.md</c> §41). No asigna unidades ni cambia pasajeros.
/// </summary>
public class EjecucionServicio
{
    private readonly AccesoServicio _acceso;
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly RegistradorUsoConductor _registradorUso;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public EjecucionServicio(
        AccesoServicio acceso,
        IServicioRepositorio servicioRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        RegistradorUsoConductor registradorUso)
    {
        _registradorUso = registradorUso;
        _acceso = acceso;
        _servicioRepositorio = servicioRepositorio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
    }

    /// <summary>Pasa el servicio a EN_CURSO y guarda cuándo y dónde empezó.</summary>
    public async Task IniciarAsync(int empresaId, int servicioId, IniciarServicioDto? datos = null)
    {
        var servicio = await _acceso.ObtenerDeLaEmpresaAsync(empresaId, servicioId);
        if (servicio is null)
        {
            throw new InvalidOperationException("El servicio indicado no existe en esta empresa.");
        }

        if (!ReglasEstadoServicio.EsTransicionValida(servicio.Estado, EstadoServicio.EN_CURSO))
        {
            throw new InvalidOperationException($"No se puede iniciar el servicio desde el estado {servicio.Estado}.");
        }

        servicio.Estado = EstadoServicio.EN_CURSO;
        servicio.HoraInicioReal = DateTime.UtcNow;
        servicio.LatitudInicio = datos?.Latitud;
        servicio.LongitudInicio = datos?.Longitud;
        await _servicioRepositorio.GuardarCambiosAsync();
    }

    /// <summary>Pasa el servicio a FINALIZADO (no se puede con pasajeros sin procesar) y anota la ruta para la facturación del mes.</summary>
    public async Task FinalizarAsync(int empresaId, int servicioId, FinalizarServicioDto? datos = null)
    {
        var servicio = await _acceso.ObtenerDeLaEmpresaAsync(empresaId, servicioId);
        if (servicio is null)
        {
            throw new InvalidOperationException("El servicio indicado no existe en esta empresa.");
        }

        if (!ReglasEstadoServicio.EsTransicionValida(servicio.Estado, EstadoServicio.FINALIZADO))
        {
            throw new InvalidOperationException($"No se puede finalizar el servicio desde el estado {servicio.Estado}.");
        }

        // Vale igual para entradas y salidas: cada pasajero debe quedar recogido o con incidencia antes de cerrar la ruta.
        var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicioId);
        if (pasajeros.Any(p => !ReglasEstadoServicioPasajero.EstaProcesado(p.Estado)))
        {
            throw new InvalidOperationException(
                "No se puede finalizar el servicio: existen pasajeros pendientes de procesar.");
        }

        servicio.Estado = EstadoServicio.FINALIZADO;
        servicio.HoraFinReal = DateTime.UtcNow;
        // El registro para facturación se guarda en el mismo guardado que la finalización: o quedan los dos o ninguno.
        await _registradorUso.RegistrarAsync(servicio, pasajeros, servicio.HoraFinReal.Value);
        servicio.LatitudFinalizacion = datos?.Latitud;
        servicio.LongitudFinalizacion = datos?.Longitud;
        await _servicioRepositorio.GuardarCambiosAsync();
    }
}
