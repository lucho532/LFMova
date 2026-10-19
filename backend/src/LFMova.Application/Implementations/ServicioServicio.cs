using LFMova.Application.DTOs.Servicios;
using LFMova.Application.Implementations.Servicios;
using LFMova.Application.Interfaces;
using LFMova.Application.Mappers;
using LFMova.Application.Validators;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de creación y gestión del ciclo de vida de
/// servicios, incluida la asignación individual de su unidad operativa (ver
/// <c>tasks.md</c> T066/T067). Aplica <see cref="ReglasMultiempresa"/> para
/// garantizar que la jornada y la sede de un servicio pertenezcan a la misma
/// empresa y <see cref="ReglasEstadoServicio"/> para validar las transiciones
/// de estado; la asignación de unidad, la ejecución y los cambios sobre rutas
/// ya armadas los delega en los colaboradores de <c>Implementations/Servicios</c>.
/// No decide autorización: eso se verifica en la capa de Api.
/// </summary>
public class ServicioServicio : IServicioServicio
{
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IJornadaRepositorio _jornadaRepositorio;
    private readonly ISedeRepositorio _sedeRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly AccesoServicio _acceso;
    private readonly AsignadorUnidadServicio _asignadorUnidad;
    private readonly ModificadorRutaArmada _modificadorRuta;
    private readonly EjecucionServicio _ejecucion;

    /// <summary>Crea el servicio con sus repositorios y colaboradores.</summary>
    public ServicioServicio(
        IServicioRepositorio servicioRepositorio,
        IJornadaRepositorio jornadaRepositorio,
        ISedeRepositorio sedeRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        AccesoServicio acceso,
        AsignadorUnidadServicio asignadorUnidad,
        ModificadorRutaArmada modificadorRuta,
        EjecucionServicio ejecucion)
    {
        _servicioRepositorio = servicioRepositorio;
        _jornadaRepositorio = jornadaRepositorio;
        _sedeRepositorio = sedeRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _acceso = acceso;
        _asignadorUnidad = asignadorUnidad;
        _modificadorRuta = modificadorRuta;
        _ejecucion = ejecucion;
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
            await _asignadorUnidad.AsegurarUnidadAsignableAsync(empresaId, datos.UnidadOperativaId.Value, servicio);
            servicio.UnidadOperativaId = datos.UnidadOperativaId;
        }

        await _servicioRepositorio.AgregarAsync(servicio);
        await _servicioRepositorio.GuardarCambiosAsync();

        return ServicioMapper.AServicioDto(servicio, empresaId);
    }

    /// <inheritdoc />
    public async Task<ServicioDto?> ObtenerPorIdAsync(int empresaId, int servicioId)
    {
        var servicio = await _acceso.ObtenerDeLaEmpresaAsync(empresaId, servicioId);
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
        var servicio = await _acceso.ObtenerDeLaEmpresaAsync(empresaId, servicioId);
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
    public async Task<int?> ObtenerUsuarioIdConductorAsignadoAsync(int empresaId, int servicioId)
    {
        var servicio = await _acceso.ObtenerDeLaEmpresaAsync(empresaId, servicioId);
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
    public Task AsignarUnidadAsync(int empresaId, int servicioId, AsignarUnidadServicioDto datos)
        => _asignadorUnidad.AsignarUnidadAsync(empresaId, servicioId, datos);

    /// <inheritdoc />
    public Task DespublicarAsync(int empresaId, int servicioId)
        => _modificadorRuta.DespublicarAsync(empresaId, servicioId);

    /// <inheritdoc />
    public Task EliminarAsync(int empresaId, int servicioId)
        => _modificadorRuta.EliminarAsync(empresaId, servicioId);

    /// <inheritdoc />
    public Task IniciarAsync(int empresaId, int servicioId, IniciarServicioDto? datos = null)
        => _ejecucion.IniciarAsync(empresaId, servicioId, datos);

    /// <inheritdoc />
    public Task FinalizarAsync(int empresaId, int servicioId, FinalizarServicioDto? datos = null)
        => _ejecucion.FinalizarAsync(empresaId, servicioId, datos);
}
