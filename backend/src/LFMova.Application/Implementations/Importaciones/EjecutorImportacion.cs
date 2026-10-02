using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.DTOs.Jornadas;
using LFMova.Application.DTOs.Servicios;
using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Guarda lo que trae un archivo ya analizado: asegura empleados y programaciones, abre la jornada de
/// cada fecha, crea los servicios y asigna los pasajeros. Reutiliza los servicios de aplicación
/// existentes para aplicar las mismas validaciones que el alta manual. No es una transacción única: si
/// falla a mitad, lo ya guardado se conserva. No interpreta el archivo ni registra la importación.
/// </summary>
public class EjecutorImportacion
{
    private readonly IJornadaServicio _jornadaServicio;
    private readonly IServicioServicio _servicioServicio;
    private readonly IServicioPasajeroServicio _servicioPasajeroServicio;
    private readonly IProgramacionTransporteServicio _programacionServicio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IZonaRepositorio _zonaRepositorio;
    private readonly ResolutorSedeImportacion _resolutorSede;
    private readonly CargadorJornadasImportacion _cargadorJornadas;
    private readonly AgrupadorPendientesRuta _agrupador;
    private readonly PlanificadorDestinosRuta _planificador;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public EjecutorImportacion(
        IJornadaServicio jornadaServicio,
        IServicioServicio servicioServicio,
        IServicioPasajeroServicio servicioPasajeroServicio,
        IProgramacionTransporteServicio programacionServicio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IZonaRepositorio zonaRepositorio,
        ResolutorSedeImportacion resolutorSede,
        CargadorJornadasImportacion cargadorJornadas,
        AgrupadorPendientesRuta agrupador,
        PlanificadorDestinosRuta planificador)
    {
        _jornadaServicio = jornadaServicio;
        _servicioServicio = servicioServicio;
        _servicioPasajeroServicio = servicioPasajeroServicio;
        _programacionServicio = programacionServicio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _zonaRepositorio = zonaRepositorio;
        _resolutorSede = resolutorSede;
        _cargadorJornadas = cargadorJornadas;
        _agrupador = agrupador;
        _planificador = planificador;
    }

    /// <summary>
    /// Ejecuta la importación del archivo analizado y va dejando los contadores y advertencias en
    /// <paramref name="resultado"/>. Si se indica una unidad, se asigna a todos los servicios; si se
    /// pide repartir, cada ruta se divide entre las unidades libres.
    /// </summary>
    public async Task EjecutarAsync(
        int empresaId, AnalisisImportacion analisis, DateOnly fechaOperativa, int? unidadOperativaId, bool repartirEntreUnidades, int? sedeGeneralId, ResultadoImportacionDto resultado)
    {
        // Una jornada por fecha operativa: si la hoja trae la fecha de cada fila, cada día va a su propia jornada.
        var contexto = new ContextoImportacion
        {
            EmpresaId = empresaId,
            Resultado = resultado,
            JornadasExistentes = await _jornadaServicio.ObtenerPorEmpresaAsync(empresaId),
            Programaciones = await _programacionServicio.ObtenerPorEmpresaAsync(empresaId),
            Sedes = await _resolutorSede.ObtenerActivasAsync(empresaId),
            Unidades = repartirEntreUnidades ? await _planificador.ObtenerUnidadesDisponiblesAsync(empresaId) : new List<UnidadDisponible>(),
            MapaZonas = repartirEntreUnidades ? MapaZonas.Crear(await _zonaRepositorio.ObtenerPorEmpresaAsync(empresaId)) : MapaZonas.Vacio
        };

        try
        {
            // Primera pasada: reúne por ruta real a quienes todavía no son pasajeros de ningún servicio.
            var rutasPendientes = await _agrupador.AgruparAsync(contexto, analisis, fechaOperativa, sedeGeneralId);

            // Segunda pasada: por cada ruta real ya unificada, reparte entre unidades (o usa la indicada) y crea el servicio.
            foreach (var (clave, ruta) in rutasPendientes)
            {
                var (jornada, servicios) = await _cargadorJornadas.ObtenerAsync(contexto, clave.ClaveJornada);
                var destinos = repartirEntreUnidades
                    ? await _planificador.PlanificarAsync(contexto, clave, ruta, servicios)
                    : new List<DestinoRuta> { new(unidadOperativaId, ruta.Pendientes, null) };

                await AsignarDestinosAsync(contexto, clave, jornada, servicios, destinos, reutilizarServicioDelHorario: !repartirEntreUnidades);
            }
        }
        finally
        {
            // Aunque la importación falle a mitad (por ejemplo, por falta de unidades), lo ya creado avanza de estado y no queda en Borrador.
            await AvanzarEstadoDeServiciosAsync(empresaId, contexto.ServiciosNuevos);
        }
    }

    /// <summary>
    /// Crea el servicio de cada destino (o usa el existente) y le asigna sus pasajeros. Cuando no se
    /// reparte entre unidades, se reutiliza el servicio que ya haya para ese mismo horario, sede y tipo.
    /// </summary>
    private async Task AsignarDestinosAsync(
        ContextoImportacion contexto, ClaveRuta clave, JornadaDto jornada, List<ServicioDto> servicios, List<DestinoRuta> destinos, bool reutilizarServicioDelHorario)
    {
        var empresaId = contexto.EmpresaId;
        var resultado = contexto.Resultado;

        foreach (var (unidad, filas, servicioExistente) in destinos)
        {
            var servicio = servicioExistente ?? (reutilizarServicioDelHorario
                ? servicios.FirstOrDefault(s =>
                    s.SedeId == clave.SedeId && s.Tipo == clave.Tipo && s.Fecha == clave.FechaServicio && s.HoraProgramada == clave.Hora
                    && s.Estado is not (EstadoServicio.FINALIZADO or EstadoServicio.CANCELADO))
                : null);

            if (servicio is null)
            {
                servicio = await _servicioServicio.CrearAsync(empresaId, jornada.JornadaId, new CrearServicioDto
                {
                    SedeId = clave.SedeId,
                    Fecha = clave.FechaServicio,
                    HoraProgramada = clave.Hora,
                    Tipo = clave.Tipo,
                    UnidadOperativaId = unidad
                });
                servicios.Add(servicio);
                contexto.ServiciosNuevos.Add(servicio);
                resultado.ServiciosCreados++;
            }

            foreach (var pendiente in filas)
            {
                try
                {
                    await _servicioPasajeroServicio.CrearAsync(
                        empresaId, servicio.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = pendiente.Programacion.ProgramacionTransporteId });
                    resultado.PasajerosAsignados++;
                }
                catch (InvalidOperationException)
                {
                    // Entre que se armó la lista de pendientes (primera pasada) y este punto, otra
                    // importación o acción concurrente sobre la misma fecha pudo haber asignado esta
                    // misma programación a un servicio: si es así, no es un error real, solo se
                    // descarta esta fila en vez de abortar toda la importación con Estado=ERROR.
                    if (await _servicioPasajeroRepositorio.ObtenerPorProgramacionAsync(pendiente.Programacion.ProgramacionTransporteId) is null)
                    {
                        throw;
                    }

                    resultado.FilasOmitidas++;
                }
            }
        }
    }

    private async Task AvanzarEstadoDeServiciosAsync(int empresaId, List<ServicioDto> serviciosNuevos)
    {
        foreach (var servicioNuevo in serviciosNuevos)
        {
            await _servicioServicio.CambiarEstadoAsync(empresaId, servicioNuevo.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
            if (servicioNuevo.UnidadOperativaId is not null)
            {
                await _servicioServicio.CambiarEstadoAsync(empresaId, servicioNuevo.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.ASIGNADO });
            }
        }
    }
}
