using LFMova.Application.DTOs.Servicios;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.Servicios;

/// <summary>
/// Asigna o cambia la unidad operativa de un servicio, validando con
/// <see cref="ReglasUnidadOperativa"/> que la unidad pueda hacerlo (ver <c>AGENTS.md</c> §19 y §25) y
/// avisando cuando cambia el conductor de una ruta ya visible. Solo modifica
/// <c>Servicio.UnidadOperativaId</c> y, si corresponde, el estado; nunca la jornada del servicio.
/// </summary>
public class AsignadorUnidadServicio
{
    private readonly AccesoServicio _acceso;
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly INotificacionServicio _notificacionServicio;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public AsignadorUnidadServicio(
        AccesoServicio acceso,
        IServicioRepositorio servicioRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        INotificacionServicio notificacionServicio)
    {
        _acceso = acceso;
        _servicioRepositorio = servicioRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _notificacionServicio = notificacionServicio;
    }

    /// <summary>Asigna la unidad al servicio; si todavía no tenía conductor, pasa a ASIGNADO.</summary>
    public async Task AsignarUnidadAsync(int empresaId, int servicioId, AsignarUnidadServicioDto datos)
    {
        var servicio = await _acceso.ObtenerDeLaEmpresaAsync(empresaId, servicioId);
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

    /// <summary>
    /// Valida que la unidad operativa indicada pueda asignarse al servicio:
    /// debe existir y estar activa, su conductor debe tener una vinculación
    /// activa con la empresa, y no debe generar conflicto temporal con otro
    /// servicio ya asignado a esa misma unidad. Devuelve el conductor
    /// resuelto para evitar volver a consultarlo.
    /// </summary>
    public async Task<Conductor> AsegurarUnidadAsignableAsync(int empresaId, int unidadOperativaId, Servicio candidato)
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
}
