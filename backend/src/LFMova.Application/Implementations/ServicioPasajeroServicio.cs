using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Application.Implementations.ServiciosPasajero;
using LFMova.Application.Interfaces;
using LFMova.Application.Mappers;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de asignación de una programación a un
/// servicio y de gestión de la participación del pasajero. Aplica
/// <see cref="ReglasServicioPasajero"/> para garantizar la consistencia entre
/// la programación, el servicio y el empleado. Resuelve aquí la asignación y
/// delega el resto en los colaboradores de
/// <c>Implementations/ServiciosPasajero</c> (consultas, participación,
/// ubicación y reorganización). No decide autorización: eso se verifica en la
/// capa de Api.
/// </summary>
public class ServicioPasajeroServicio : IServicioPasajeroServicio
{
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IProgramacionTransporteRepositorio _programacionRepositorio;
    private readonly AccesoServicioPasajero _acceso;
    private readonly ConsultasServicioPasajero _consultas;
    private readonly GestorParticipacionPasajero _gestorParticipacion;
    private readonly GestorUbicacionPasajero _gestorUbicacion;
    private readonly ReorganizadorPasajeros _reorganizador;

    /// <summary>Crea el servicio con sus repositorios y colaboradores.</summary>
    public ServicioPasajeroServicio(
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IProgramacionTransporteRepositorio programacionRepositorio,
        AccesoServicioPasajero acceso,
        ConsultasServicioPasajero consultas,
        GestorParticipacionPasajero gestorParticipacion,
        GestorUbicacionPasajero gestorUbicacion,
        ReorganizadorPasajeros reorganizador)
    {
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _programacionRepositorio = programacionRepositorio;
        _acceso = acceso;
        _consultas = consultas;
        _gestorParticipacion = gestorParticipacion;
        _gestorUbicacion = gestorUbicacion;
        _reorganizador = reorganizador;
    }

    /// <inheritdoc />
    public async Task<ServicioPasajeroDto> CrearAsync(int empresaId, int servicioId, CrearServicioPasajeroDto datos)
    {
        var servicio = await _acceso.ObtenerServicioDeLaEmpresaOFallarAsync(empresaId, servicioId);

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
    public async Task ReordenarAsync(int empresaId, int servicioPasajeroId, ReordenarServicioPasajeroDto datos)
    {
        var servicioPasajero = await _acceso.ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);

        servicioPasajero.Orden = datos.NuevoOrden;
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public Task<List<ServicioPasajeroDto>> ObtenerPorServicioAsync(int empresaId, int servicioId)
        => _consultas.ObtenerPorServicioAsync(empresaId, servicioId);

    /// <inheritdoc />
    public Task<List<ServicioDelEmpleadoDto>> ObtenerPorUsuarioEmpleadoAsync(int usuarioId)
        => _consultas.ObtenerPorUsuarioEmpleadoAsync(usuarioId);

    /// <inheritdoc />
    public Task<int?> ObtenerUsuarioIdEmpleadoAsync(int empresaId, int servicioPasajeroId)
        => _consultas.ObtenerUsuarioIdEmpleadoAsync(empresaId, servicioPasajeroId);

    /// <inheritdoc />
    public Task<int?> ObtenerUsuarioIdConductorAsync(int empresaId, int servicioPasajeroId)
        => _consultas.ObtenerUsuarioIdConductorAsync(empresaId, servicioPasajeroId);

    /// <inheritdoc />
    public Task ConfirmarAsync(int empresaId, int servicioPasajeroId, ConfirmarServicioPasajeroDto datos)
        => _gestorParticipacion.ConfirmarAsync(empresaId, servicioPasajeroId, datos);

    /// <inheritdoc />
    public Task MarcarNoAsistiraAsync(int empresaId, int servicioPasajeroId)
        => _gestorParticipacion.MarcarNoAsistiraAsync(empresaId, servicioPasajeroId);

    /// <inheritdoc />
    public Task MarcarLlegadaAsync(int empresaId, int servicioPasajeroId)
        => _gestorParticipacion.MarcarLlegadaAsync(empresaId, servicioPasajeroId);

    /// <inheritdoc />
    public Task CambiarEstadoAsync(int empresaId, int servicioPasajeroId, CambiarEstadoServicioPasajeroDto datos)
        => _gestorParticipacion.CambiarEstadoAsync(empresaId, servicioPasajeroId, datos);

    /// <inheritdoc />
    public Task CompartirUbicacionAsync(int empresaId, int servicioPasajeroId, CompartirUbicacionDto datos)
        => _gestorUbicacion.CompartirUbicacionAsync(empresaId, servicioPasajeroId, datos);

    /// <inheritdoc />
    public Task GuardarUbicacionRecogidaAsync(int empresaId, int servicioPasajeroId, CompartirUbicacionDto datos)
        => _gestorUbicacion.GuardarUbicacionRecogidaAsync(empresaId, servicioPasajeroId, datos);

    /// <inheritdoc />
    public Task<List<UbicacionAnteriorDto>> ObtenerUbicacionesAnterioresAsync(int empresaId, int servicioPasajeroId)
        => _gestorUbicacion.ObtenerUbicacionesAnterioresAsync(empresaId, servicioPasajeroId);

    /// <inheritdoc />
    public Task EliminarUbicacionGuardadaAsync(int empresaId, int servicioPasajeroId)
        => _gestorUbicacion.EliminarUbicacionGuardadaAsync(empresaId, servicioPasajeroId);

    /// <inheritdoc />
    public Task EliminarAsync(int empresaId, int servicioPasajeroId)
        => _reorganizador.EliminarAsync(empresaId, servicioPasajeroId);

    /// <inheritdoc />
    public Task EditarDireccionAsync(int empresaId, int servicioPasajeroId, EditarDireccionServicioPasajeroDto datos)
        => _reorganizador.EditarDireccionAsync(empresaId, servicioPasajeroId, datos);

    /// <inheritdoc />
    public Task MoverAsync(int empresaId, int servicioPasajeroId, MoverServicioPasajeroDto datos)
        => _reorganizador.MoverAsync(empresaId, servicioPasajeroId, datos);
}
