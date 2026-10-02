using LFMova.Application.DTOs.Incidencias;
using LFMova.Application.Interfaces;
using LFMova.Application.Mappers;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa el registro de incidencias y evidencias sobre un
/// <c>ServicioPasajero</c> durante la ejecución de un servicio (ver
/// <c>data-model.md</c> §19/§20 y <c>tasks.md</c> T091/T092/T093). No decide
/// autorización de acceso al endpoint: eso se verifica en la capa de Api.
/// </summary>
public class IncidenciaServicio : IIncidenciaServicio
{
    private readonly IIncidenciaRepositorio _incidenciaRepositorio;
    private readonly IEvidenciaRepositorio _evidenciaRepositorio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;

    /// <summary>Crea el servicio con sus repositorios.</summary>
    public IncidenciaServicio(
        IIncidenciaRepositorio incidenciaRepositorio,
        IEvidenciaRepositorio evidenciaRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IServicioRepositorio servicioRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio)
    {
        _incidenciaRepositorio = incidenciaRepositorio;
        _evidenciaRepositorio = evidenciaRepositorio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _servicioRepositorio = servicioRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
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
    public async Task<IncidenciaDto> CrearAsync(int empresaId, int servicioPasajeroId, CrearIncidenciaDto datos)
    {
        var servicioPasajero = await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);

        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(servicioPasajero.ServicioId);
        if (servicio is null || servicio.Estado != EstadoServicio.EN_CURSO)
        {
            throw new InvalidOperationException("Solo se puede reportar una incidencia mientras el servicio está en curso.");
        }

        if (servicioPasajero.Estado != EstadoServicioPasajero.CONDUCTOR_LLEGO)
        {
            throw new InvalidOperationException("Solo se puede reportar una incidencia después de marcar la llegada al pasajero (\"He llegado\").");
        }

        var incidencia = new Incidencia
        {
            ServicioPasajeroId = servicioPasajero.ServicioPasajeroId,
            Tipo = datos.Tipo,
            Descripcion = datos.Descripcion,
            FechaHora = DateTime.UtcNow,
            Latitud = datos.Latitud,
            Longitud = datos.Longitud
        };

        await _incidenciaRepositorio.AgregarAsync(incidencia);
        await _incidenciaRepositorio.GuardarCambiosAsync();

        // Cualquier incidencia deja al pasajero como NO_RECOGIDO: el conductor no necesita un botón aparte.
        if (ReglasEstadoServicioPasajero.PuedeQuedarNoRecogido(servicioPasajero.Estado))
        {
            servicioPasajero.Estado = EstadoServicioPasajero.NO_RECOGIDO;
            servicioPasajero.HoraProcesado = DateTime.UtcNow;
            await _servicioPasajeroRepositorio.GuardarCambiosAsync();
        }

        return IncidenciaMapper.AIncidenciaDto(incidencia);
    }

    /// <inheritdoc />
    public async Task<List<IncidenciaDto>> ObtenerPorServicioPasajeroAsync(int empresaId, int servicioPasajeroId)
    {
        await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);

        var incidencias = await _incidenciaRepositorio.ObtenerPorServicioPasajeroAsync(servicioPasajeroId);
        var resultado = new List<IncidenciaDto>();
        foreach (var incidencia in incidencias)
        {
            var evidencias = await _evidenciaRepositorio.ObtenerPorIncidenciaAsync(incidencia.IncidenciaId);
            resultado.Add(IncidenciaMapper.AIncidenciaDto(incidencia, evidencias));
        }

        return resultado;
    }

    /// <inheritdoc />
    public async Task<List<IncidenciaDto>> ObtenerPorServicioAsync(int empresaId, int jornadaId, int servicioId)
    {
        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(servicioId);
        if (servicio is null || servicio.JornadaId != jornadaId || servicio.Jornada is null ||
            !ReglasMultiempresa.JornadaPerteneceAEmpresa(servicio.Jornada, empresaId))
        {
            throw new InvalidOperationException("El servicio indicado no existe en esta empresa.");
        }

        var incidencias = await _incidenciaRepositorio.ObtenerPorServicioIdAsync(servicioId);
        var resultado = new List<IncidenciaDto>();
        foreach (var incidencia in incidencias)
        {
            var evidencias = await _evidenciaRepositorio.ObtenerPorIncidenciaAsync(incidencia.IncidenciaId);
            resultado.Add(IncidenciaMapper.AIncidenciaDto(incidencia, evidencias));
        }

        return resultado;
    }

    /// <inheritdoc />
    public async Task<EvidenciaDto> AgregarEvidenciaAsync(int empresaId, int servicioPasajeroId, int incidenciaId, AgregarEvidenciaDto datos)
    {
        var incidencia = await _incidenciaRepositorio.ObtenerPorIdAsync(incidenciaId);
        if (incidencia is null || incidencia.ServicioPasajeroId != servicioPasajeroId)
        {
            throw new InvalidOperationException("La incidencia indicada no existe para este pasajero.");
        }

        await ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);

        var evidencia = new Evidencia
        {
            IncidenciaId = incidenciaId,
            Tipo = datos.Tipo,
            ReferenciaArchivo = datos.ReferenciaArchivo,
            FechaHora = DateTime.UtcNow
        };

        await _evidenciaRepositorio.AgregarAsync(evidencia);
        await _evidenciaRepositorio.GuardarCambiosAsync();

        return EvidenciaMapper.AEvidenciaDto(evidencia);
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
