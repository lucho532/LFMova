using LFMova.Application.DTOs.Chat;
using LFMova.Application.Interfaces;
using LFMova.Application.Mappers;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa la conversación individual entre el conductor y el empleado de
/// un <c>ServicioPasajero</c> (ver <c>spec.md</c> §30/§34 y <c>tasks.md</c>
/// T087). Crea la conversación de forma perezosa, al primer mensaje. No
/// implementa chat grupal. Es de un solo sentido: el conductor envía solo mensajes
/// predeterminados y el pasajero únicamente los lee (<see cref="ReglasChat"/>). No decide autorización de acceso al endpoint: eso
/// se verifica en la capa de Api.
/// </summary>
public class ChatServicio : IChatServicio
{
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IConversacionRepositorio _conversacionRepositorio;
    private readonly IMensajeRepositorio _mensajeRepositorio;
    private readonly INotificacionServicio? _notificacionServicio;

    /// <summary>Crea el servicio con sus repositorios.</summary>
    public ChatServicio(
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IServicioRepositorio servicioRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        IConversacionRepositorio conversacionRepositorio,
        IMensajeRepositorio mensajeRepositorio,
        INotificacionServicio? notificacionServicio = null)
    {
        _notificacionServicio = notificacionServicio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _servicioRepositorio = servicioRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _conversacionRepositorio = conversacionRepositorio;
        _mensajeRepositorio = mensajeRepositorio;
    }

    /// <inheritdoc />
    public async Task<(int? UsuarioIdEmpleado, int? UsuarioIdConductor)> ObtenerParticipantesAsync(int empresaId, int servicioPasajeroId)
    {
        var servicioPasajero = await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(servicioPasajero.EmpleadoId);

        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(servicioPasajero.ServicioId);
        int? usuarioIdConductor = null;
        if (servicio?.UnidadOperativaId is not null)
        {
            var unidadOperativa = await _unidadOperativaRepositorio.ObtenerPorIdAsync(servicio.UnidadOperativaId.Value);
            if (unidadOperativa is not null)
            {
                var conductor = await _conductorRepositorio.ObtenerPorIdAsync(unidadOperativa.ConductorId);
                usuarioIdConductor = conductor?.UsuarioId;
            }
        }

        return (empleado?.UsuarioId, usuarioIdConductor);
    }

    /// <inheritdoc />
    public async Task<List<MensajeDto>> ObtenerMensajesAsync(int empresaId, int servicioPasajeroId)
    {
        var conversacion = await _conversacionRepositorio.ObtenerPorServicioPasajeroAsync(servicioPasajeroId);
        if (conversacion is null)
        {
            return [];
        }

        var mensajes = await _mensajeRepositorio.ObtenerPorConversacionAsync(conversacion.ConversacionId);
        return mensajes.Select(MensajeMapper.AMensajeDto).ToList();
    }

    /// <inheritdoc />
    public async Task<MensajeDto> EnviarMensajeAsync(int empresaId, int servicioPasajeroId, int usuarioIdRemitente, EnviarMensajeDto datos)
    {
        var (usuarioIdEmpleado, usuarioIdConductor) = await ObtenerParticipantesAsync(empresaId, servicioPasajeroId);
        if (usuarioIdRemitente != usuarioIdEmpleado && usuarioIdRemitente != usuarioIdConductor)
        {
            throw new InvalidOperationException("El usuario indicado no participa en esta conversación.");
        }

        // Chat de un solo sentido (ver ReglasChat): el pasajero solo lee y el conductor solo envía mensajes predeterminados.
        if (usuarioIdRemitente != usuarioIdConductor)
        {
            throw new InvalidOperationException("El pasajero solo puede leer los mensajes de su conductor.");
        }

        var servicioPasajero = await _servicioPasajeroRepositorio.ObtenerPorIdAsync(servicioPasajeroId);
        var servicio = servicioPasajero is null ? null : await _servicioRepositorio.ObtenerPorIdAsync(servicioPasajero.ServicioId);
        if (servicio?.Estado == EstadoServicio.FINALIZADO)
        {
            throw new InvalidOperationException("Esta ruta ya finalizó: el chat quedó cerrado para enviar mensajes nuevos.");
        }

        if (!ReglasChat.EsMensajePredeterminado(datos.Contenido))
        {
            throw new InvalidOperationException("Solo se pueden enviar los mensajes predeterminados.");
        }

        var conversacion = await _conversacionRepositorio.ObtenerPorServicioPasajeroAsync(servicioPasajeroId);
        if (conversacion is null)
        {
            conversacion = new Conversacion { ServicioPasajeroId = servicioPasajeroId };
            await _conversacionRepositorio.AgregarAsync(conversacion);
            await _conversacionRepositorio.GuardarCambiosAsync();
        }

        var mensaje = new Mensaje
        {
            ConversacionId = conversacion.ConversacionId,
            UsuarioId = usuarioIdRemitente,
            Contenido = datos.Contenido,
            FechaHora = DateTime.UtcNow
        };

        await _mensajeRepositorio.AgregarAsync(mensaje);
        await _mensajeRepositorio.GuardarCambiosAsync();

        await NotificarMensajeAsync(servicioPasajeroId, usuarioIdEmpleado, datos.Contenido);

        return MensajeMapper.AMensajeDto(mensaje);
    }

    /// <summary>Avisa al pasajero que su conductor le escribió (el chat no es en tiempo real).</summary>
    private async Task NotificarMensajeAsync(int servicioPasajeroId, int? usuarioIdEmpleado, string contenido)
    {
        if (_notificacionServicio is null || usuarioIdEmpleado is null)
        {
            return;
        }

        await _notificacionServicio.CrearAsync(
            usuarioIdEmpleado.Value, "MENSAJE_NUEVO", "Mensaje de tu conductor", contenido, $"/mi-transporte/chat/{servicioPasajeroId}");
    }

    private async Task<ServicioPasajero> ObtenerPasajeroDeLaEmpresaOFallarAsync(int empresaId, int servicioPasajeroId)
    {
        var servicioPasajero = await _servicioPasajeroRepositorio.ObtenerPorIdAsync(servicioPasajeroId);
        if (servicioPasajero is null)
        {
            throw new InvalidOperationException("El pasajero indicado no existe.");
        }

        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(servicioPasajero.ServicioId);
        if (servicio is null || servicio.Jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(servicio.Jornada, empresaId))
        {
            throw new InvalidOperationException("El pasajero indicado no existe en esta empresa.");
        }

        return servicioPasajero;
    }
}
