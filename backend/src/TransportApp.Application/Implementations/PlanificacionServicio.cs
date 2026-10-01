using TransportApp.Application.DTOs.Planificacion;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Domain.Rules;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa la generación de propuestas de planificación asistida (ver
/// <c>spec.md</c> §28, <c>data-model.md</c> §30 y <c>tasks.md</c> Fase 14).
/// Aplica <see cref="ReglasPlanificacion"/> sobre datos reales del servicio;
/// nunca persiste ni modifica la planificación definitiva. No decide
/// autorización: eso se verifica en la capa de Api.
/// </summary>
public class PlanificacionServicio : IPlanificacionServicio
{
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly ISedeRepositorio _sedeRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IVehiculoRepositorio _vehiculoRepositorio;
    private readonly OpcionesPlanificacion _opciones;

    /// <summary>Crea el servicio con sus repositorios y su configuración.</summary>
    public PlanificacionServicio(
        IServicioRepositorio servicioRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        ISedeRepositorio sedeRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IVehiculoRepositorio vehiculoRepositorio,
        OpcionesPlanificacion opciones)
    {
        _servicioRepositorio = servicioRepositorio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _sedeRepositorio = sedeRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _vehiculoRepositorio = vehiculoRepositorio;
        _opciones = opciones;
    }

    /// <inheritdoc />
    public async Task<PropuestaPlanificacionDto> GenerarPropuestaAsync(int empresaId, int servicioId)
    {
        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(servicioId);
        if (servicio is null || servicio.Jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(servicio.Jornada, empresaId))
        {
            throw new InvalidOperationException("El servicio indicado no existe en esta empresa.");
        }

        var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicioId);
        var sede = await _sedeRepositorio.ObtenerPorIdAsync(servicio.SedeId);

        var propuesta = new PropuestaPlanificacionDto { ServicioId = servicioId };

        await AgregarAdvertenciaDeCapacidadAsync(servicio, pasajeros.Count, propuesta);

        var pasajerosOrdenados = servicio.Tipo == TipoServicio.ENTRADA
            ? ReglasPlanificacion.OrdenarParaEntrada(pasajeros, sede?.Latitud, sede?.Longitud)
            : pasajeros.OrderBy(p => p.Orden).ToList();

        if (pasajeros.Any(p => p.Latitud is null || p.Longitud is null))
        {
            propuesta.Advertencias.Add(
                "Uno o más pasajeros no tienen coordenadas registradas; el orden propuesto para ellos es aproximado.");
        }

        propuesta.OrdenPropuesto = pasajerosOrdenados
            .Select((pasajero, indice) => new PasajeroPropuestoDto
            {
                ServicioPasajeroId = pasajero.ServicioPasajeroId,
                EmpleadoId = pasajero.EmpleadoId,
                Orden = indice + 1,
                DistanciaALaSedeKm = ReglasPlanificacion.CalcularDistanciaKm(
                    pasajero.Latitud, pasajero.Longitud, sede?.Latitud, sede?.Longitud)
            })
            .ToList();

        if (servicio.Tipo == TipoServicio.ENTRADA)
        {
            var horaLimite = ReglasPlanificacion.HoraLimiteLlegadaSede(servicio.HoraProgramada);
            propuesta.HoraLimiteLlegadaSede = horaLimite;
            propuesta.HoraSugeridaInicioRecogida = ReglasPlanificacion.HoraSugeridaInicioRecogida(
                horaLimite, _opciones.VentanaRecogidaMinutos);
        }

        await AgregarContinuidadGeograficaAsync(servicio, sede, pasajerosOrdenados, propuesta);

        return propuesta;
    }

    /// <summary>
    /// Advierte si la cantidad de pasajeros excede la capacidad del vehículo
    /// de la unidad asignada (ver <c>tasks.md</c> T077). No bloquea: la
    /// asignación real se decide y valida en <see cref="IServicioServicio"/>.
    /// </summary>
    private async Task AgregarAdvertenciaDeCapacidadAsync(Servicio servicio, int cantidadPasajeros, PropuestaPlanificacionDto propuesta)
    {
        if (servicio.UnidadOperativaId is null)
        {
            propuesta.Advertencias.Add("El servicio no tiene una unidad operativa asignada; no fue posible validar su capacidad.");
            return;
        }

        var unidadOperativa = await _unidadOperativaRepositorio.ObtenerPorIdAsync(servicio.UnidadOperativaId.Value);
        var vehiculo = unidadOperativa is null ? null : await _vehiculoRepositorio.ObtenerPorIdAsync(unidadOperativa.VehiculoId);
        if (vehiculo is null)
        {
            return;
        }

        if (ReglasPlanificacion.ExcedeCapacidad(cantidadPasajeros, vehiculo.Capacidad))
        {
            propuesta.Advertencias.Add(
                $"La cantidad de pasajeros ({cantidadPasajeros}) excede la capacidad del vehículo asignado ({vehiculo.Capacidad}).");
        }
    }

    /// <summary>
    /// Calcula la distancia hacia el servicio anterior y hacia el siguiente
    /// de la misma unidad operativa el mismo día (ver <c>tasks.md</c> T078).
    /// Es información de apoyo para el coordinador; nunca bloquea la
    /// propuesta.
    /// </summary>
    private async Task AgregarContinuidadGeograficaAsync(
        Servicio servicio, Sede? sede, List<ServicioPasajero> pasajerosOrdenados, PropuestaPlanificacionDto propuesta)
    {
        if (servicio.UnidadOperativaId is null)
        {
            return;
        }

        var serviciosDeLaUnidad = await _servicioRepositorio.ObtenerPorUnidadOperativaAsync(servicio.UnidadOperativaId.Value);
        var adyacentesElMismoDia = serviciosDeLaUnidad
            .Where(s => s.ServicioId != servicio.ServicioId && s.Estado != EstadoServicio.CANCELADO && s.Fecha == servicio.Fecha)
            .OrderBy(s => s.HoraProgramada)
            .ToList();

        var anterior = adyacentesElMismoDia.LastOrDefault(s => s.HoraProgramada < servicio.HoraProgramada);
        var siguiente = adyacentesElMismoDia.FirstOrDefault(s => s.HoraProgramada > servicio.HoraProgramada);

        var primeraUbicacionActual = servicio.Tipo == TipoServicio.ENTRADA
            ? PrimeraUbicacion(pasajerosOrdenados, sede)
            : (sede?.Latitud, sede?.Longitud);

        var ultimaUbicacionActual = servicio.Tipo == TipoServicio.ENTRADA
            ? (sede?.Latitud, sede?.Longitud)
            : UltimaUbicacion(pasajerosOrdenados, sede);

        if (anterior is not null)
        {
            var ubicacionFinalAnterior = await ObtenerUbicacionFinalAsync(anterior);
            propuesta.DistanciaDesdeServicioAnteriorKm = ReglasPlanificacion.CalcularDistanciaKm(
                ubicacionFinalAnterior.Latitud, ubicacionFinalAnterior.Longitud,
                primeraUbicacionActual.Item1, primeraUbicacionActual.Item2);
        }

        if (siguiente is not null)
        {
            var primeraUbicacionSiguiente = await ObtenerPrimeraUbicacionAsync(siguiente);
            propuesta.DistanciaHaciaServicioSiguienteKm = ReglasPlanificacion.CalcularDistanciaKm(
                ultimaUbicacionActual.Item1, ultimaUbicacionActual.Item2,
                primeraUbicacionSiguiente.Latitud, primeraUbicacionSiguiente.Longitud);
        }
    }

    /// <summary>
    /// Ubicación final de un servicio adyacente: la sede si es
    /// <c>ENTRADA</c> (termina en la sede), o la última parada registrada si
    /// es <c>SALIDA</c>.
    /// </summary>
    private async Task<(double? Latitud, double? Longitud)> ObtenerUbicacionFinalAsync(Servicio servicioAdyacente)
    {
        if (servicioAdyacente.Tipo == TipoServicio.ENTRADA)
        {
            var sedeAdyacente = await _sedeRepositorio.ObtenerPorIdAsync(servicioAdyacente.SedeId);
            return (sedeAdyacente?.Latitud, sedeAdyacente?.Longitud);
        }

        var pasajerosAdyacente = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicioAdyacente.ServicioId);
        return UltimaUbicacion(pasajerosAdyacente, sede: null);
    }

    /// <summary>
    /// Primera ubicación de un servicio adyacente: la sede si es
    /// <c>SALIDA</c> (parte de la sede), o la primera recogida registrada si
    /// es <c>ENTRADA</c>.
    /// </summary>
    private async Task<(double? Latitud, double? Longitud)> ObtenerPrimeraUbicacionAsync(Servicio servicioAdyacente)
    {
        if (servicioAdyacente.Tipo == TipoServicio.SALIDA)
        {
            var sedeAdyacente = await _sedeRepositorio.ObtenerPorIdAsync(servicioAdyacente.SedeId);
            return (sedeAdyacente?.Latitud, sedeAdyacente?.Longitud);
        }

        var pasajerosAdyacente = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicioAdyacente.ServicioId);
        return PrimeraUbicacion(pasajerosAdyacente, sede: null);
    }

    private static (double? Latitud, double? Longitud) PrimeraUbicacion(List<ServicioPasajero> pasajeros, Sede? sede)
    {
        var primero = pasajeros.OrderBy(p => p.Orden).FirstOrDefault();
        return primero is null ? (sede?.Latitud, sede?.Longitud) : (primero.Latitud, primero.Longitud);
    }

    private static (double? Latitud, double? Longitud) UltimaUbicacion(List<ServicioPasajero> pasajeros, Sede? sede)
    {
        var ultimo = pasajeros.OrderByDescending(p => p.Orden).FirstOrDefault();
        return ultimo is null ? (sede?.Latitud, sede?.Longitud) : (ultimo.Latitud, ultimo.Longitud);
    }
}
