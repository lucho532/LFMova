using System.Globalization;
using System.Text;
using TransportApp.Application.DTOs.Servicios;
using TransportApp.Application.DTOs.ServiciosPasajero;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Mappers;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Domain.Rules;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de asignación de una programación a un
/// servicio y de gestión de la participación del pasajero. Aplica
/// <see cref="ReglasServicioPasajero"/> para garantizar la consistencia entre
/// la programación, el servicio y el empleado. No decide autorización: eso
/// se verifica en la capa de Api.
/// </summary>
public class ServicioPasajeroServicio : IServicioPasajeroServicio
{
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IProgramacionTransporteRepositorio _programacionRepositorio;
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly IUbicacionRecogidaHistoricaRepositorio _ubicacionHistoricaRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly INotificacionServicio _notificacionServicio;
    private readonly IServicioServicio _servicioServicio;
    private readonly IZonaRepositorio _zonaRepositorio;
    private readonly ICorredorVialRepositorio _corredorVialRepositorio;
    private readonly IBarreraGeograficaRepositorio _barreraGeograficaRepositorio;

    /// <summary>Crea el servicio con sus repositorios.</summary>
    public ServicioPasajeroServicio(
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IProgramacionTransporteRepositorio programacionRepositorio,
        IServicioRepositorio servicioRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IUbicacionRecogidaHistoricaRepositorio ubicacionHistoricaRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        INotificacionServicio notificacionServicio,
        IServicioServicio servicioServicio,
        IZonaRepositorio zonaRepositorio,
        ICorredorVialRepositorio corredorVialRepositorio,
        IBarreraGeograficaRepositorio barreraGeograficaRepositorio)
    {
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _programacionRepositorio = programacionRepositorio;
        _servicioRepositorio = servicioRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _ubicacionHistoricaRepositorio = ubicacionHistoricaRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _notificacionServicio = notificacionServicio;
        _servicioServicio = servicioServicio;
        _zonaRepositorio = zonaRepositorio;
        _corredorVialRepositorio = corredorVialRepositorio;
        _barreraGeograficaRepositorio = barreraGeograficaRepositorio;
    }

    /// <inheritdoc />
    public async Task<ServicioPasajeroDto> CrearAsync(int empresaId, int servicioId, CrearServicioPasajeroDto datos)
    {
        var servicio = await ObtenerServicioDeLaEmpresaOFallarAsync(empresaId, servicioId);

        var programacion = await _programacionRepositorio.ObtenerPorIdAsync(datos.ProgramacionTransporteId);
        if (programacion is null || !ReglasServicioPasajero.ProgramacionEsDelMismoContextoEmpresarial(programacion, empresaId))
        {
            throw new InvalidOperationException("La programación indicada no existe en esta empresa.");
        }

        if (!ReglasServicioPasajero.ProgramacionCoincideConSedeDelServicio(programacion, servicio))
        {
            throw new InvalidOperationException("La sede de la programación no coincide con la sede del servicio.");
        }

        if (await _servicioPasajeroRepositorio.ObtenerPorProgramacionAsync(datos.ProgramacionTransporteId) is not null)
        {
            throw new InvalidOperationException("Esta programación ya fue asignada a un servicio.");
        }

        var pasajerosDelServicio = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicioId);
        var siguienteOrden = pasajerosDelServicio.Count == 0 ? 1 : pasajerosDelServicio.Max(p => p.Orden) + 1;

        var servicioPasajero = new ServicioPasajero
        {
            ServicioId = servicioId,
            ProgramacionTransporteId = datos.ProgramacionTransporteId,
            EmpleadoId = programacion.EmpleadoId,
            Estado = EstadoServicioPasajero.PROGRAMADO,
            Orden = siguienteOrden,
            DireccionRecogida = programacion.DireccionRecogida,
            Latitud = datos.Latitud,
            Longitud = datos.Longitud
        };

        await _servicioPasajeroRepositorio.AgregarAsync(servicioPasajero);
        try
        {
            await _servicioPasajeroRepositorio.GuardarCambiosAsync();
        }
        catch (Exception)
        {
            // Entre la comprobación de arriba y este guardado, otra petición concurrente (por ejemplo, dos
            // importaciones solapadas del mismo archivo) pudo haber asignado esta misma programación
            // primero: se descarta este intento fallido (si no, el contexto lo reintentaría en el próximo
            // guardado y volvería a fallar) y se reporta el mismo error de negocio de siempre.
            _servicioPasajeroRepositorio.Descartar(servicioPasajero);
            if (await _servicioPasajeroRepositorio.ObtenerPorProgramacionAsync(datos.ProgramacionTransporteId) is null)
            {
                throw;
            }

            throw new InvalidOperationException("Esta programación ya fue asignada a un servicio.");
        }

        return ServicioPasajeroMapper.AServicioPasajeroDto(servicioPasajero);
    }

    /// <inheritdoc />
    public async Task<List<ServicioPasajeroDto>> ObtenerPorServicioAsync(int empresaId, int servicioId)
    {
        await ObtenerServicioDeLaEmpresaOFallarAsync(empresaId, servicioId);

        var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicioId);
        var resultado = new List<ServicioPasajeroDto>();
        foreach (var pasajero in pasajeros)
        {
            var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(pasajero.EmpleadoId);
            resultado.Add(ServicioPasajeroMapper.AServicioPasajeroDto(pasajero, empleado));
        }

        return resultado;
    }

    /// <inheritdoc />
    public async Task ConfirmarAsync(int empresaId, int servicioPasajeroId, ConfirmarServicioPasajeroDto datos)
    {
        var servicioPasajero = await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);

        if (!string.IsNullOrWhiteSpace(datos.NuevaDireccion))
        {
            if (datos.EstablecerComoHabitual)
            {
                await ConservarDireccionHabitualComoHistoricaYReemplazarAsync(servicioPasajero.EmpleadoId, datos);
            }

            servicioPasajero.DireccionRecogida = datos.NuevaDireccion;
            servicioPasajero.Latitud = datos.Latitud;
            servicioPasajero.Longitud = datos.Longitud;
        }

        servicioPasajero.Estado = EstadoServicioPasajero.CONFIRMADO;
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        await NotificarConductorAsync(servicioPasajero, "PASAJERO_CONFIRMO", "Un pasajero confirmó", "confirmó que asistirá");
    }

    /// <inheritdoc />
    public async Task MarcarNoAsistiraAsync(int empresaId, int servicioPasajeroId)
    {
        var servicioPasajero = await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);

        servicioPasajero.Estado = EstadoServicioPasajero.NO_ASISTIRA;
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        await NotificarConductorAsync(servicioPasajero, "PASAJERO_NO_ASISTIRA", "Un pasajero no asistirá", "indicó que no asistirá");
    }

    /// <inheritdoc />
    public async Task ReordenarAsync(int empresaId, int servicioPasajeroId, ReordenarServicioPasajeroDto datos)
    {
        var servicioPasajero = await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);

        servicioPasajero.Orden = datos.NuevoOrden;
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task MarcarLlegadaAsync(int empresaId, int servicioPasajeroId)
    {
        var servicioPasajero = await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(servicioPasajero.ServicioId);
        if (servicio is null || servicio.Estado != EstadoServicio.EN_CURSO)
        {
            throw new InvalidOperationException("Solo se puede registrar la llegada mientras el servicio está en curso.");
        }

        if (servicioPasajero.Estado != EstadoServicioPasajero.PROGRAMADO && servicioPasajero.Estado != EstadoServicioPasajero.CONFIRMADO)
        {
            throw new InvalidOperationException($"No se puede registrar la llegada desde el estado {servicioPasajero.Estado}.");
        }

        servicioPasajero.Estado = EstadoServicioPasajero.CONDUCTOR_LLEGO;
        servicioPasajero.HoraLlegadaConductor = DateTime.UtcNow;
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        await NotificarEmpleadoAsync(servicioPasajero, "CONDUCTOR_LLEGO", "Tu conductor llegó", "Tu conductor llegó al punto de recogida y te está esperando.");
    }

    /// <inheritdoc />
    public async Task CambiarEstadoAsync(int empresaId, int servicioPasajeroId, CambiarEstadoServicioPasajeroDto datos)
    {
        var servicioPasajero = await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        if (!ReglasEstadoServicioPasajero.EsTransicionDeResultadoValida(servicioPasajero.Estado, datos.NuevoEstado))
        {
            throw new InvalidOperationException($"No se puede pasar el pasajero de {servicioPasajero.Estado} a {datos.NuevoEstado}.");
        }

        servicioPasajero.Estado = datos.NuevoEstado;
        servicioPasajero.HoraProcesado = DateTime.UtcNow;
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        if (datos.NuevoEstado == EstadoServicioPasajero.RECOGIDO)
        {
            await GuardarUbicacionSiEsLaPrimeraVezAsync(servicioPasajero);
            await NotificarEmpleadoAsync(servicioPasajero, "PASAJERO_RECOGIDO", "Ya vas en camino", "Tu conductor te recogió. ¡Buen viaje!");
        }
    }

    /// <summary>
    /// Si el empleado todavía no tiene ninguna ubicación guardada y este
    /// pasajero sí tiene un punto GPS conocido (compartido por él o guardado
    /// antes por el conductor), lo deja registrado como su ubicación de
    /// recogida habitual, sin que el conductor tenga que guardarlo a mano.
    /// </summary>
    private async Task GuardarUbicacionSiEsLaPrimeraVezAsync(ServicioPasajero servicioPasajero)
    {
        if (servicioPasajero.Latitud is null || servicioPasajero.Longitud is null)
        {
            return;
        }

        var yaTieneUbicacionGuardada = (await _ubicacionHistoricaRepositorio.ObtenerPorEmpleadoAsync(servicioPasajero.EmpleadoId)).Count > 0;
        if (yaTieneUbicacionGuardada)
        {
            return;
        }

        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(servicioPasajero.EmpleadoId);
        await _ubicacionHistoricaRepositorio.AgregarAsync(new UbicacionRecogidaHistorica
        {
            EmpleadoId = servicioPasajero.EmpleadoId,
            Direccion = servicioPasajero.DireccionRecogida,
            Barrio = empleado?.Barrio ?? string.Empty,
            Latitud = servicioPasajero.Latitud,
            Longitud = servicioPasajero.Longitud,
            FechaRegistro = DateTime.UtcNow
        });
        await _ubicacionHistoricaRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task CompartirUbicacionAsync(int empresaId, int servicioPasajeroId, CompartirUbicacionDto datos)
    {
        var servicioPasajero = await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        servicioPasajero.Latitud = datos.Latitud;
        servicioPasajero.Longitud = datos.Longitud;
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        // Sin esta notificación, la pantalla del conductor solo se enteraría en el siguiente sondeo (hasta
        // 15 s después): si mientras tanto le da a "Navegar", el botón usaría un punto desactualizado.
        await NotificarConductorAsync(servicioPasajero, "UBICACION_COMPARTIDA", "Ubicación compartida", "compartió su ubicación en vivo");
    }

    /// <inheritdoc />
    public async Task GuardarUbicacionRecogidaAsync(int empresaId, int servicioPasajeroId, CompartirUbicacionDto datos)
    {
        var servicioPasajero = await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(servicioPasajero.EmpleadoId)
            ?? throw new InvalidOperationException("El empleado indicado no existe.");

        servicioPasajero.Latitud = datos.Latitud;
        servicioPasajero.Longitud = datos.Longitud;

        await _ubicacionHistoricaRepositorio.AgregarAsync(new UbicacionRecogidaHistorica
        {
            EmpleadoId = empleado.EmpleadoId,
            Direccion = servicioPasajero.DireccionRecogida,
            Barrio = empleado.Barrio,
            Latitud = datos.Latitud,
            Longitud = datos.Longitud,
            FechaRegistro = DateTime.UtcNow
        });

        await _ubicacionHistoricaRepositorio.GuardarCambiosAsync();
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task<List<UbicacionAnteriorDto>> ObtenerUbicacionesAnterioresAsync(int empresaId, int servicioPasajeroId)
    {
        var servicioPasajero = await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        var historicas = await _ubicacionHistoricaRepositorio.ObtenerPorEmpleadoAsync(servicioPasajero.EmpleadoId);
        return historicas
            .Select(u => new UbicacionAnteriorDto { Direccion = u.Direccion, Barrio = u.Barrio, Latitud = u.Latitud, Longitud = u.Longitud, FechaRegistro = u.FechaRegistro })
            .ToList();
    }

    /// <inheritdoc />
    public async Task EliminarUbicacionGuardadaAsync(int empresaId, int servicioPasajeroId)
    {
        var servicioPasajero = await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        await _ubicacionHistoricaRepositorio.EliminarPorEmpleadoAsync(servicioPasajero.EmpleadoId);
        await _ubicacionHistoricaRepositorio.GuardarCambiosAsync();
    }

    private async Task ConservarDireccionHabitualComoHistoricaYReemplazarAsync(int empleadoId, ConfirmarServicioPasajeroDto datos)
    {
        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(empleadoId);
        if (empleado is null)
        {
            throw new InvalidOperationException("El empleado indicado no existe.");
        }

        var ubicacionAnterior = new UbicacionRecogidaHistorica
        {
            EmpleadoId = empleado.EmpleadoId,
            Direccion = empleado.Direccion,
            Barrio = empleado.Barrio,
            FechaRegistro = DateTime.UtcNow
        };

        await _ubicacionHistoricaRepositorio.AgregarAsync(ubicacionAnterior);

        empleado.Direccion = datos.NuevaDireccion!;
        empleado.Barrio = datos.NuevoBarrio ?? empleado.Barrio;

        await _ubicacionHistoricaRepositorio.GuardarCambiosAsync();
    }

    private async Task NotificarConductorAsync(ServicioPasajero servicioPasajero, string tipo, string titulo, string accion)
    {
        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(servicioPasajero.ServicioId);
        if (servicio?.UnidadOperativaId is null)
        {
            return;
        }

        var unidadOperativa = await _unidadOperativaRepositorio.ObtenerPorIdAsync(servicio.UnidadOperativaId.Value);
        if (unidadOperativa is null)
        {
            return;
        }

        var conductor = await _conductorRepositorio.ObtenerPorIdAsync(unidadOperativa.ConductorId);
        if (conductor is null)
        {
            return;
        }

        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(servicioPasajero.EmpleadoId);
        await _notificacionServicio.CrearAsync(
            conductor.UsuarioId,
            tipo,
            titulo,
            $"{empleado?.NombreCompleto ?? "El pasajero"} {accion} en el servicio del {servicio.Fecha:yyyy-MM-dd} a las {servicio.HoraProgramada:HH\\:mm}.",
            servicio.Jornada is null ? null : $"/conductor/servicios/{servicio.Jornada.EmpresaId}/{servicio.JornadaId}/{servicio.ServicioId}/chat/{servicioPasajero.ServicioPasajeroId}");
    }

    private async Task NotificarEmpleadoAsync(ServicioPasajero servicioPasajero, string tipo, string titulo, string mensaje)
    {
        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(servicioPasajero.EmpleadoId);
        if (empleado is null)
        {
            return;
        }

        await _notificacionServicio.CrearAsync(empleado.UsuarioId, tipo, titulo, mensaje, "/mi-transporte");
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task EliminarAsync(int empresaId, int servicioPasajeroId)
    {
        var pasajero = await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        var servicio = await ObtenerServicioDeLaEmpresaOFallarAsync(empresaId, pasajero.ServicioId);
        if (servicio.Estado is EstadoServicio.EN_CURSO or EstadoServicio.FINALIZADO)
        {
            throw new InvalidOperationException("No se puede eliminar un pasajero de una ruta en curso o finalizada.");
        }

        await _servicioPasajeroRepositorio.EliminarAsync(pasajero);
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        // Igual que al mover el último pasajero de una ruta a otra (MoverAsync): si esta era la última
        // persona del servicio, ya no representa nada operativo, así que se elimina también.
        var pasajerosRestantes = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicio.ServicioId);
        if (pasajerosRestantes.Count == 0)
        {
            await LiberarServicioVacioAsync(empresaId, servicio);
        }
    }

    /// <inheritdoc />
    public async Task EditarDireccionAsync(int empresaId, int servicioPasajeroId, EditarDireccionServicioPasajeroDto datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Direccion))
        {
            throw new InvalidOperationException("La dirección no puede quedar vacía.");
        }

        var pasajero = await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(pasajero.ServicioId);
        if (servicio is not null && servicio.Estado is EstadoServicio.EN_CURSO or EstadoServicio.FINALIZADO)
        {
            throw new InvalidOperationException("No se puede editar un pasajero de una ruta en curso o finalizada.");
        }

        pasajero.DireccionRecogida = datos.Direccion.Trim();
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task MoverAsync(int empresaId, int servicioPasajeroId, MoverServicioPasajeroDto datos)
    {
        var pasajero = await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        var servicioActual = await ObtenerServicioDeLaEmpresaOFallarAsync(empresaId, pasajero.ServicioId);

        if (servicioActual.Estado is EstadoServicio.CANCELADO or EstadoServicio.EN_CURSO or EstadoServicio.FINALIZADO)
        {
            throw new InvalidOperationException("No se puede mover un pasajero de una ruta cancelada, en curso o finalizada.");
        }

        if (datos.ServicioDestinoId == servicioActual.ServicioId)
        {
            throw new InvalidOperationException("El pasajero ya está en esa ruta.");
        }

        var destino = await ObtenerServicioDeLaEmpresaOFallarAsync(empresaId, datos.ServicioDestinoId);
        if (destino.Estado is EstadoServicio.CANCELADO or EstadoServicio.EN_CURSO or EstadoServicio.FINALIZADO)
        {
            throw new InvalidOperationException("La ruta destino está cancelada, en curso o finalizada.");
        }

        if (destino.SedeId != servicioActual.SedeId || destino.Tipo != servicioActual.Tipo
            || destino.Fecha != servicioActual.Fecha || destino.HoraProgramada != servicioActual.HoraProgramada)
        {
            throw new InvalidOperationException("Solo se puede mover un pasajero a otra ruta de la misma sede, fecha, hora y tipo.");
        }

        var pasajerosDestino = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(destino.ServicioId);
        pasajero.ServicioId = destino.ServicioId;
        pasajero.Orden = pasajerosDestino.Count == 0 ? 1 : pasajerosDestino.Max(p => p.Orden) + 1;
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        await AprenderCorredorAsync(empresaId, pasajero, destino);

        var pasajerosRestantesEnOrigen = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicioActual.ServicioId);
        if (pasajerosRestantesEnOrigen.Count == 0)
        {
            await LiberarServicioVacioAsync(empresaId, servicioActual);
        }
    }

    /// <summary>
    /// Un servicio que se quedó sin pasajeros (porque el último se movió a otra ruta) ya no representa
    /// nada operativo, así que se elimina. Si tenía una unidad operativa asignada, esa unidad queda
    /// libre para la primera ruta de la misma jornada que esté pendiente de conductor (se prefiere una de
    /// la misma sede, fecha, hora y tipo, por si quedó gente sin asignar justo en ese horario), pero nunca
    /// una cuya fecha y hora coincidan con otra ruta que esa misma unidad ya tenga en la jornada (un
    /// conductor no puede estar en dos rutas a la vez, ver <see cref="Domain.Rules.ReglasUnidadOperativa.HayConflictoTemporal"/>);
    /// si no hay ninguna ruta pendiente compatible, el conductor simplemente queda disponible para una futura importación.
    /// </summary>
    private async Task LiberarServicioVacioAsync(int empresaId, Servicio servicioVacio)
    {
        var unidadLiberada = servicioVacio.UnidadOperativaId;
        var jornadaId = servicioVacio.JornadaId;

        await _servicioRepositorio.EliminarAsync(servicioVacio);
        await _servicioRepositorio.GuardarCambiosAsync();

        if (unidadLiberada is null)
        {
            return;
        }

        var serviciosDeLaJornada = await _servicioRepositorio.ObtenerPorJornadaAsync(jornadaId);

        var rutasQueOcupanALaUnidad = serviciosDeLaJornada.Where(s => s.UnidadOperativaId == unidadLiberada).ToList();

        var pendientesDeConductor = serviciosDeLaJornada
            .Where(s => s.UnidadOperativaId is null && s.Estado == EstadoServicio.PENDIENTE_ASIGNACION
                && !ReglasUnidadOperativa.HayConflictoTemporal(s, rutasQueOcupanALaUnidad))
            .ToList();

        var candidato = pendientesDeConductor.FirstOrDefault(s =>
            s.SedeId == servicioVacio.SedeId && s.Tipo == servicioVacio.Tipo && s.Fecha == servicioVacio.Fecha && s.HoraProgramada == servicioVacio.HoraProgramada)
            ?? pendientesDeConductor.OrderBy(s => s.HoraProgramada).FirstOrDefault();

        if (candidato is null)
        {
            return;
        }

        await _servicioServicio.AsignarUnidadAsync(empresaId, candidato.ServicioId, new AsignarUnidadServicioDto { UnidadOperativaId = unidadLiberada.Value });
    }

    /// <summary>
    /// Cuando el coordinador mueve un pasajero de una ruta a otra, eso es información real sobre qué
    /// barrios pueden compartir vehículo: si la zona del barrio del pasajero movido y las de los demás
    /// pasajeros de la ruta destino son distintas y todavía no comparten corredor vial, se crea (o se
    /// amplía) automáticamente un <see cref="CorredorVial"/> que las une, para que las próximas
    /// importaciones ya las repartan juntas sin que el coordinador tenga que declararlo a mano. Nunca
    /// une zonas cuyos barrios tengan una <see cref="BarreraGeografica"/> declarada entre sí, nunca
    /// fusiona dos corredores ya existentes y distintos entre sí, y un corredor deja de crecer por
    /// aprendizaje automático al llegar a <see cref="MaximoZonasPorCorredorAprendido"/> zonas (esas
    /// decisiones sí las toma el coordinador a mano desde Zonas). Si el barrio de algún pasajero no está
    /// clasificado en ninguna zona, simplemente no hay nada que aprender de él.
    /// </summary>
    private async Task AprenderCorredorAsync(int empresaId, ServicioPasajero pasajeroMovido, Servicio destino)
    {
        var empleadoMovido = await _empleadoRepositorio.ObtenerPorIdAsync(pasajeroMovido.EmpleadoId);
        if (empleadoMovido is null)
        {
            return;
        }

        var zonas = (await _zonaRepositorio.ObtenerPorEmpresaAsync(empresaId)).Where(z => z.Activa).ToList();
        var zonaMovido = ResolverZonaPorBarrio(empleadoMovido.Barrio, zonas);
        if (zonaMovido is null)
        {
            return;
        }

        var pasajerosDestino = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(destino.ServicioId);
        var barreras = await _barreraGeograficaRepositorio.ObtenerPorEmpresaAsync(empresaId);
        var zonasOtrasYaVinculadas = new HashSet<int>();

        foreach (var pasajeroDestino in pasajerosDestino)
        {
            if (pasajeroDestino.ServicioPasajeroId == pasajeroMovido.ServicioPasajeroId)
            {
                continue;
            }

            var empleadoDestino = await _empleadoRepositorio.ObtenerPorIdAsync(pasajeroDestino.EmpleadoId);
            var zonaOtra = empleadoDestino is null ? null : ResolverZonaPorBarrio(empleadoDestino.Barrio, zonas);
            if (zonaOtra is null || zonaOtra.ZonaId == zonaMovido.ZonaId || !zonasOtrasYaVinculadas.Add(zonaOtra.ZonaId))
            {
                continue;
            }

            await VincularZonasSiCorrespondeAsync(empresaId, zonaMovido, zonaOtra, zonas, barreras);
        }
    }

    private static Zona? ResolverZonaPorBarrio(string barrio, List<Zona> zonas)
    {
        var normalizado = Normalizar(barrio);
        return zonas.FirstOrDefault(z => z.Barrios.Any(b => Normalizar(b) == normalizado));
    }

    /// <summary>Tope de zonas que un corredor vial puede acumular por aprendizaje automático: pasado esto, agrandarlo es una decisión manual del coordinador desde Zonas, no algo que un solo movimiento deba decidir por sí solo.</summary>
    private const int MaximoZonasPorCorredorAprendido = 4;

    private async Task VincularZonasSiCorrespondeAsync(int empresaId, Zona zonaA, Zona zonaB, List<Zona> todasLasZonas, List<BarreraGeografica> barreras)
    {
        if (zonaA.CorredorVialId is not null && zonaA.CorredorVialId == zonaB.CorredorVialId)
        {
            return;
        }

        if (zonaA.CorredorVialId is not null && zonaB.CorredorVialId is not null)
        {
            return;
        }

        var todosLosBarrios = zonaA.Barrios.Concat(zonaB.Barrios).ToList();
        if (ReglasBarrerasGeograficas.BuscarParConBarrera(todosLosBarrios, barreras) is not null)
        {
            return;
        }

        // Un corredor ya aprendido no sigue creciendo sin límite: que dos zonas hayan compartido vehículo
        // una vez no dice nada sobre una tercera zona que solo coincide en que también comparte ESE
        // corredor con una de las dos. Pasado el tope, ampliarlo pasa a ser una decisión manual.
        if (zonaA.CorredorVialId is not null && todasLasZonas.Count(z => z.CorredorVialId == zonaA.CorredorVialId) >= MaximoZonasPorCorredorAprendido)
        {
            return;
        }

        if (zonaB.CorredorVialId is not null && todasLasZonas.Count(z => z.CorredorVialId == zonaB.CorredorVialId) >= MaximoZonasPorCorredorAprendido)
        {
            return;
        }

        if (zonaA.CorredorVialId is not null)
        {
            zonaB.CorredorVialId = zonaA.CorredorVialId;
            await _zonaRepositorio.GuardarCambiosAsync();
            return;
        }

        if (zonaB.CorredorVialId is not null)
        {
            zonaA.CorredorVialId = zonaB.CorredorVialId;
            await _zonaRepositorio.GuardarCambiosAsync();
            return;
        }

        var corredor = new CorredorVial { EmpresaId = empresaId, Nombre = $"{zonaA.Nombre} - {zonaB.Nombre}", Activo = true };
        await _corredorVialRepositorio.AgregarAsync(corredor);
        await _corredorVialRepositorio.GuardarCambiosAsync();

        zonaA.CorredorVialId = corredor.CorredorVialId;
        zonaB.CorredorVialId = corredor.CorredorVialId;
        await _zonaRepositorio.GuardarCambiosAsync();
    }

    private static string Normalizar(string texto)
    {
        var descompuesto = texto.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        return new string(descompuesto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }

    private async Task<Servicio> ObtenerServicioDeLaEmpresaOFallarAsync(int empresaId, int servicioId)
    {
        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(servicioId);
        if (servicio is null || servicio.Jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(servicio.Jornada, empresaId))
        {
            throw new InvalidOperationException("El servicio indicado no existe en esta empresa.");
        }

        return servicio;
    }

    private async Task<ServicioPasajero> ObtenerPasajeroDeLaEmpresaOFallarAsync(int empresaId, int servicioPasajeroId)
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
