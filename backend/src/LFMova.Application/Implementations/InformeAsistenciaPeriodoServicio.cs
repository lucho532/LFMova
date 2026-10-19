using LFMova.Application.DTOs.Soportes;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa <see cref="IInformeAsistenciaPeriodoServicio"/>: toma los pasajeros de todas las rutas de la
/// empresa en el período, deja los que cumplen el filtro (<see cref="ReglasAsistenciaRuta"/>) y los entrega
/// en un Excel con su ruta, conductor y horarios. No modifica nada ni decide autorización de acceso.
/// </summary>
public class InformeAsistenciaPeriodoServicio : IInformeAsistenciaPeriodoServicio
{
    private readonly IInformeAsistenciaRepositorio _repositorio;
    private readonly IEmpresaRepositorio _empresaRepositorio;
    private readonly IGeneradorInformeAsistenciaPeriodo _generador;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public InformeAsistenciaPeriodoServicio(
        IInformeAsistenciaRepositorio repositorio,
        IEmpresaRepositorio empresaRepositorio,
        IGeneradorInformeAsistenciaPeriodo generador)
    {
        _repositorio = repositorio;
        _empresaRepositorio = empresaRepositorio;
        _generador = generador;
    }

    /// <inheritdoc />
    public async Task<ArchivoSoporte> GenerarAsync(int empresaId, DateOnly desde, DateOnly hasta, FiltroAsistenciaRuta filtro)
    {
        var informe = await ObtenerAsync(empresaId, desde, hasta, filtro);
        return new ArchivoSoporte(NombreArchivo(desde, hasta, filtro), _generador.Generar(informe));
    }

    /// <inheritdoc />
    public async Task<InformeAsistenciaPeriodo> ObtenerAsync(int empresaId, DateOnly desde, DateOnly hasta, FiltroAsistenciaRuta filtro)
    {
        if (hasta < desde)
        {
            throw new ArgumentException("La fecha final no puede ser anterior a la inicial.");
        }

        var empresa = await _empresaRepositorio.ObtenerPorIdAsync(empresaId);
        var informe = new InformeAsistenciaPeriodo
        {
            NombreEmpresa = empresa?.Nombre ?? string.Empty,
            Desde = desde,
            Hasta = hasta,
            Filtro = filtro
        };

        var pasajeros = await _repositorio.ObtenerPasajerosAsync(empresaId, desde, hasta);
        informe.Filas = pasajeros
            .Where(p => ReglasAsistenciaRuta.Cumple(p.Estado, filtro))
            // Por ruta y, dentro de cada una, en el orden en que se atendieron (los sin resultado, al final).
            .OrderBy(p => p.Fecha).ThenBy(p => p.HoraRuta).ThenBy(p => p.ServicioId)
            .ThenBy(p => p.HoraProcesado is null).ThenBy(p => p.HoraProcesado).ThenBy(p => p.Orden)
            .Select(ArmarFila)
            .ToList();

        return informe;
    }

    private static FilaInformeAsistenciaPeriodo ArmarFila(PasajeroDeRutaDto p) => new()
    {
        ServicioId = p.ServicioId,
        JornadaId = p.JornadaId,
        Fecha = p.Fecha,
        HoraRuta = p.HoraRuta,
        Tipo = p.Tipo,
        Sede = p.Sede,
        Conductor = p.Conductor,
        Placa = p.Placa,
        Cedula = p.Cedula,
        NombreCompleto = p.NombreCompleto,
        Telefono = p.Telefono,
        Direccion = p.Direccion,
        Barrio = p.Barrio,
        Estado = p.Estado,
        // Los instantes reales se guardan en UTC; el informe se entrega en hora de Colombia.
        HoraRegistro = p.HoraProcesado is null ? null : ReglasHoraColombia.AhoraColombia(p.HoraProcesado.Value),
        EsperaSegundos = p.HoraLlegadaConductor is null || p.HoraProcesado is null
            ? null
            : Math.Max(0, (int)(p.HoraProcesado.Value - p.HoraLlegadaConductor.Value).TotalSeconds)
    };

    private static string NombreArchivo(DateOnly desde, DateOnly hasta, FiltroAsistenciaRuta filtro)
    {
        var parte = filtro switch
        {
            FiltroAsistenciaRuta.ASISTIERON => "asistieron",
            FiltroAsistenciaRuta.NO_ASISTIERON => "no-asistieron",
            _ => "pasajeros"
        };
        var fechas = desde == hasta ? $"{desde:yyyy-MM-dd}" : $"{desde:yyyy-MM-dd}_a_{hasta:yyyy-MM-dd}";
        return $"{parte}-{fechas}.xlsx";
    }
}
