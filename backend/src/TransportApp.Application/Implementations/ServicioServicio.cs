using TransportApp.Application.DTOs.Servicios;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Utils;
using TransportApp.Application.Mappers;
using TransportApp.Application.Validators;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Domain.Rules;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de creación y gestión del ciclo de vida de
/// servicios, incluida la asignación individual de su unidad operativa (ver
/// <c>tasks.md</c> T066/T067). Aplica <see cref="ReglasMultiempresa"/> para
/// garantizar que la jornada y la sede de un servicio pertenezcan a la misma
/// empresa, <see cref="ReglasEstadoServicio"/> para validar las transiciones
/// de estado, y <see cref="ReglasUnidadOperativa"/> para validar la unidad
/// asignada. No decide autorización: eso se verifica en la capa de Api.
/// </summary>
public class ServicioServicio : IServicioServicio
{
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IJornadaRepositorio _jornadaRepositorio;
    private readonly ISedeRepositorio _sedeRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly INotificacionServicio _notificacionServicio;

    /// <summary>Crea el servicio con sus repositorios.</summary>
    public ServicioServicio(
        IServicioRepositorio servicioRepositorio,
        IJornadaRepositorio jornadaRepositorio,
        ISedeRepositorio sedeRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        INotificacionServicio notificacionServicio)
    {
        _servicioRepositorio = servicioRepositorio;
        _jornadaRepositorio = jornadaRepositorio;
        _sedeRepositorio = sedeRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _notificacionServicio = notificacionServicio;
    }

    /// <inheritdoc />
    public async Task<ServicioDto> CrearAsync(int empresaId, int jornadaId, CrearServicioDto datos)
    {
        var jornada = await _jornadaRepositorio.ObtenerPorIdAsync(jornadaId);
        if (jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(jornada, empresaId))
        {
            throw new InvalidOperationException("La jornada indicada no existe en esta empresa.");
        }

        var sede = await _sedeRepositorio.ObtenerPorIdAsync(datos.SedeId);
        if (sede is null || !ReglasMultiempresa.ServicioEsConsistenteConEmpresa(jornada, sede, empresaId))
        {
            throw new InvalidOperationException("La sede indicada no existe en esta empresa.");
        }

        if (!FechaProgramacionValidador.EsValida(datos.Fecha))
        {
            throw new InvalidOperationException("La fecha del servicio es obligatoria.");
        }

        if (!TipoServicioValidador.EsValido(datos.Tipo))
        {
            throw new InvalidOperationException("El tipo de servicio indicado no es válido.");
        }

        var servicio = new Servicio
        {
            JornadaId = jornadaId,
            SedeId = datos.SedeId,
            Fecha = datos.Fecha,
            HoraProgramada = datos.HoraProgramada,
            Tipo = datos.Tipo,
            Estado = EstadoServicio.BORRADOR
        };

        if (datos.UnidadOperativaId is not null)
        {
            await AsegurarUnidadAsignableAsync(empresaId, datos.UnidadOperativaId.Value, servicio);
            servicio.UnidadOperativaId = datos.UnidadOperativaId;
        }

        await _servicioRepositorio.AgregarAsync(servicio);
        await _servicioRepositorio.GuardarCambiosAsync();

        return ServicioMapper.AServicioDto(servicio, empresaId);
    }

    /// <inheritdoc />
    public async Task<ServicioDto?> ObtenerPorIdAsync(int empresaId, int servicioId)
    {
        var servicio = await ObtenerServicioDeLaEmpresaAsync(empresaId, servicioId);
        return servicio is null ? null : ServicioMapper.AServicioDto(servicio, empresaId);
    }

    /// <inheritdoc />
    public async Task<List<ServicioDto>> ObtenerPorJornadaAsync(int empresaId, int jornadaId)
    {
        var jornada = await _jornadaRepositorio.ObtenerPorIdAsync(jornadaId);
        if (jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(jornada, empresaId))
        {
            throw new InvalidOperationException("La jornada indicada no existe en esta empresa.");
        }

        var servicios = await _servicioRepositorio.ObtenerPorJornadaAsync(jornadaId);
        return servicios.Select(s => ServicioMapper.AServicioDto(s, empresaId)).ToList();
    }

    /// <inheritdoc />
    public async Task<List<ServicioDto>> ObtenerPendientesDeProgramacionAsync(int empresaId, DateOnly desde)
    {
        var servicios = await _servicioRepositorio.ObtenerPendientesDeProgramacionAsync(empresaId, desde);
        return servicios
            .OrderBy(s => s.Fecha).ThenBy(s => s.HoraProgramada)
            .Select(s => ServicioMapper.AServicioDto(s, empresaId))
            .ToList();
    }

    /// <inheritdoc />
    public async Task CambiarEstadoAsync(int empresaId, int servicioId, CambiarEstadoServicioDto datos)
    {
        var servicio = await ObtenerServicioDeLaEmpresaAsync(empresaId, servicioId);
        if (servicio is null)
        {
            throw new InvalidOperationException("El servicio indicado no existe en esta empresa.");
        }

        if (!ReglasEstadoServicio.EsTransicionValida(servicio.Estado, datos.NuevoEstado))
        {
            throw new InvalidOperationException($"No se puede pasar el servicio de {servicio.Estado} a {datos.NuevoEstado}.");
        }

        servicio.Estado = datos.NuevoEstado;
        await _servicioRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task AsignarUnidadAsync(int empresaId, int servicioId, AsignarUnidadServicioDto datos)
    {
        var servicio = await ObtenerServicioDeLaEmpresaAsync(empresaId, servicioId);
        if (servicio is null)
        {
            throw new InvalidOperationException("El servicio indicado no existe en esta empresa.");
        }

        var conductorAnteriorId = servicio.UnidadOperativaId is null
            ? (int?)null
            : (await _unidadOperativaRepositorio.ObtenerPorIdAsync(servicio.UnidadOperativaId.Value))?.ConductorId;

        var conductorNuevo = await AsegurarUnidadAsignableAsync(empresaId, datos.UnidadOperativaId, servicio);

        // La reasignación modifica exclusivamente UnidadOperativaId; JornadaId no cambia. Si el servicio
        // todavía no tenía conductor (recién creado o sin unidad hasta ahora), esta asignación lo resuelve:
        // pasa a ASIGNADO. Si ya estaba asignado, publicado, etc., solo cambia el conductor, sin tocar su estado.
        servicio.UnidadOperativaId = datos.UnidadOperativaId;
        if (servicio.Estado is EstadoServicio.BORRADOR or EstadoServicio.PENDIENTE_ASIGNACION)
        {
            servicio.Estado = EstadoServicio.ASIGNADO;
        }

        await _servicioRepositorio.GuardarCambiosAsync();

        // Solo se avisa si la ruta ya la ven el conductor y los pasajeros (publicada o en curso): mientras el
        // coordinador la arma, cambiarle el conductor es trabajo interno y todos se enteran al publicarla.
        if (conductorAnteriorId is not null && conductorAnteriorId != conductorNuevo.ConductorId
            && ReglasEstadoServicio.EsVisibleParaConductorYEmpleado(servicio.Estado))
        {
            await NotificarCambioDeConductorAsync(servicio, conductorNuevo.UsuarioId);
        }
    }

    /// <summary>
    /// Notifica al nuevo conductor y a cada empleado participante del
    /// servicio cuando la reasignación de unidad cambia efectivamente el
    /// conductor responsable (ver <c>data-model.md</c> §23 y <c>tasks.md</c>
    /// T074). El conductor anterior no se notifica.
    /// </summary>
    private async Task NotificarCambioDeConductorAsync(Servicio servicio, int usuarioIdConductorNuevo)
    {
        await _notificacionServicio.CrearAsync(
            usuarioIdConductorNuevo,
            "CAMBIO_CONDUCTOR",
            "Nueva ruta asignada",
            $"Se te asignó la {FormatoOperacion.DescribirRuta(servicio)}.");

        var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicio.ServicioId);
        foreach (var pasajero in pasajeros)
        {
            var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(pasajero.EmpleadoId);
            if (empleado is null)
            {
                continue;
            }

            await _notificacionServicio.CrearAsync(
                empleado.UsuarioId,
                "CAMBIO_CONDUCTOR",
                "Cambió tu conductor asignado",
                $"Tu {FormatoOperacion.DescribirRuta(servicio)} ahora la hace otro conductor.");
        }
    }

    /// <inheritdoc />
    public async Task DespublicarAsync(int empresaId, int servicioId)
    {
        var servicio = await ObtenerServicioDeLaEmpresaAsync(empresaId, servicioId);
        if (servicio is null)
        {
            throw new InvalidOperationException("El servicio indicado no existe en esta empresa.");
        }

        if (!ReglasEstadoServicio.EsTransicionValida(servicio.Estado, EstadoServicio.ASIGNADO))
        {
            throw new InvalidOperationException($"No se puede volver a editar un servicio en estado {servicio.Estado}.");
        }

        servicio.Estado = EstadoServicio.ASIGNADO;
        await _servicioRepositorio.GuardarCambiosAsync();

        if (servicio.UnidadOperativaId is not null)
        {
            var unidadOperativa = await _unidadOperativaRepositorio.ObtenerPorIdAsync(servicio.UnidadOperativaId.Value);
            var conductor = unidadOperativa is null ? null : await _conductorRepositorio.ObtenerPorIdAsync(unidadOperativa.ConductorId);
            if (conductor is not null)
            {
                await _notificacionServicio.CrearAsync(
                    conductor.UsuarioId,
                    "RUTA_MODIFICADA",
                    "Tu ruta volvió a edición",
                    $"El coordinador volvió a abrir tu {FormatoOperacion.DescribirRuta(servicio)} para hacerle cambios. Te avisaremos cuando esté lista de nuevo.");
            }
        }
    }

    /// <inheritdoc />
    public async Task EliminarAsync(int empresaId, int servicioId)
    {
        var servicio = await ObtenerServicioDeLaEmpresaAsync(empresaId, servicioId);
        if (servicio is null)
        {
            throw new InvalidOperationException("El servicio indicado no existe en esta empresa.");
        }

        if (servicio.Estado is EstadoServicio.EN_CURSO or EstadoServicio.FINALIZADO)
        {
            throw new InvalidOperationException("No se puede eliminar una ruta en curso o ya realizada.");
        }

        var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicioId);
        foreach (var pasajero in pasajeros)
        {
            await _servicioPasajeroRepositorio.EliminarAsync(pasajero);
        }
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        // Solo se avisa al conductor si ya veía la ruta (publicada): una ruta sin publicar nunca le llegó.
        var unidadOperativaId = ReglasEstadoServicio.EsVisibleParaConductorYEmpleado(servicio.Estado) ? servicio.UnidadOperativaId : null;
        var rutaEliminada = FormatoOperacion.DescribirRuta(servicio);
        await _servicioRepositorio.EliminarAsync(servicio);
        await _servicioRepositorio.GuardarCambiosAsync();

        if (unidadOperativaId is not null)
        {
            var unidadOperativa = await _unidadOperativaRepositorio.ObtenerPorIdAsync(unidadOperativaId.Value);
            var conductor = unidadOperativa is null ? null : await _conductorRepositorio.ObtenerPorIdAsync(unidadOperativa.ConductorId);
            if (conductor is not null)
            {
                await _notificacionServicio.CrearAsync(
                    conductor.UsuarioId,
                    "RUTA_MODIFICADA",
                    "Tu ruta fue eliminada",
                    $"El coordinador eliminó tu {rutaEliminada}.");
            }
        }
    }

    /// <inheritdoc />
    public async Task<int?> ObtenerUsuarioIdConductorAsignadoAsync(int empresaId, int servicioId)
    {
        var servicio = await ObtenerServicioDeLaEmpresaAsync(empresaId, servicioId);
        if (servicio?.UnidadOperativaId is null)
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
    public async Task IniciarAsync(int empresaId, int servicioId, IniciarServicioDto? datos = null)
    {
        var servicio = await ObtenerServicioDeLaEmpresaAsync(empresaId, servicioId);
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

    /// <inheritdoc />
    public async Task FinalizarAsync(int empresaId, int servicioId, FinalizarServicioDto? datos = null)
    {
        var servicio = await ObtenerServicioDeLaEmpresaAsync(empresaId, servicioId);
        if (servicio is null)
        {
            throw new InvalidOperationException("El servicio indicado no existe en esta empresa.");
        }

        if (!ReglasEstadoServicio.EsTransicionValida(servicio.Estado, EstadoServicio.FINALIZADO))
        {
            throw new InvalidOperationException($"No se puede finalizar el servicio desde el estado {servicio.Estado}.");
        }

        if (servicio.Tipo == TipoServicio.ENTRADA)
        {
            var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicioId);
            if (pasajeros.Any(p => !ReglasEstadoServicioPasajero.EstaProcesado(p.Estado)))
            {
                throw new InvalidOperationException(
                    "No se puede finalizar el servicio: existen pasajeros pendientes de procesar.");
            }
        }

        servicio.Estado = EstadoServicio.FINALIZADO;
        servicio.HoraFinReal = DateTime.UtcNow;
        servicio.LatitudFinalizacion = datos?.Latitud;
        servicio.LongitudFinalizacion = datos?.Longitud;
        await _servicioRepositorio.GuardarCambiosAsync();
    }

    /// <summary>
    /// Valida que la unidad operativa indicada pueda asignarse al servicio:
    /// debe existir y estar activa, su conductor debe tener una vinculación
    /// activa con la empresa, y no debe generar conflicto temporal con otro
    /// servicio ya asignado a esa misma unidad. Devuelve el conductor
    /// resuelto para evitar volver a consultarlo.
    /// </summary>
    private async Task<Conductor> AsegurarUnidadAsignableAsync(int empresaId, int unidadOperativaId, Servicio candidato)
    {
        var unidadOperativa = await _unidadOperativaRepositorio.ObtenerPorIdAsync(unidadOperativaId);
        if (unidadOperativa is null || !unidadOperativa.Activa)
        {
            throw new InvalidOperationException("La unidad operativa indicada no existe o no está activa.");
        }

        var conductor = await _conductorRepositorio.ObtenerPorIdAsync(unidadOperativa.ConductorId);
        if (conductor is null || !conductor.VinculacionesConductorEmpresa.Any(v => v.EmpresaId == empresaId && v.Activa))
        {
            throw new InvalidOperationException(
                "El conductor de la unidad operativa no tiene una vinculación activa con esta empresa.");
        }

        var otrosServiciosDeLaUnidad = await _servicioRepositorio.ObtenerPorUnidadOperativaAsync(unidadOperativaId);
        if (ReglasUnidadOperativa.HayConflictoTemporal(candidato, otrosServiciosDeLaUnidad))
        {
            throw new InvalidOperationException(
                "La unidad operativa ya tiene otro servicio asignado en la misma fecha y hora. Solo puede hacer a la misma hora una entrada y una salida de la misma sede.");
        }

        return conductor;
    }

    private async Task<Servicio?> ObtenerServicioDeLaEmpresaAsync(int empresaId, int servicioId)
    {
        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(servicioId);
        if (servicio is null || servicio.Jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(servicio.Jornada, empresaId))
        {
            return null;
        }

        return servicio;
    }
}
