using LFMova.Application.DTOs.Soportes;
using LFMova.Application.Implementations.Servicios;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa <see cref="IInformeAsistenciaServicio"/>: arma, a partir de lo que el conductor registró en
/// una ruta, el informe de quiénes asistieron y quiénes no, con la hora de recogida y la espera de cada
/// uno, y lo entrega como Excel. No modifica nada ni decide autorización de acceso al endpoint.
/// </summary>
public class InformeAsistenciaServicio : IInformeAsistenciaServicio
{
    private readonly AccesoServicio _acceso;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IEmpresaRepositorio _empresaRepositorio;
    private readonly IGeneradorInformeAsistencia _generador;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public InformeAsistenciaServicio(
        AccesoServicio acceso,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        IEmpresaRepositorio empresaRepositorio,
        IGeneradorInformeAsistencia generador)
    {
        _acceso = acceso;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _empresaRepositorio = empresaRepositorio;
        _generador = generador;
    }

    /// <inheritdoc />
    public async Task<ArchivoSoporte> GenerarAsync(int empresaId, int servicioId, FiltroAsistenciaRuta filtro)
    {
        var servicio = await _acceso.ObtenerDeLaEmpresaAsync(empresaId, servicioId)
            ?? throw new InvalidOperationException("El servicio indicado no existe en esta empresa.");

        var empresa = await _empresaRepositorio.ObtenerPorIdAsync(empresaId);
        var informe = new InformeAsistenciaRuta
        {
            NombreEmpresa = empresa?.Nombre ?? string.Empty,
            Tipo = servicio.Tipo,
            NombreSede = servicio.Sede?.Nombre ?? string.Empty,
            Fecha = servicio.Fecha,
            Hora = servicio.HoraProgramada,
            NombreConductor = await ObtenerNombreConductorAsync(servicio.UnidadOperativaId),
            Filtro = filtro
        };

        var ordenados = ReglasAsistenciaRuta.OrdenarCronologicamente(await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicioId));
        informe.TotalPasajerosRuta = ordenados.Count;

        // El número es la posición en la ruta completa, así que no cambia al filtrar: «No asistieron» puede empezar en el #3.
        var numero = 0;
        foreach (var pasajero in ordenados)
        {
            int? numeroPasajero = pasajero.HoraProcesado is null ? null : ++numero;
            if (!ReglasAsistenciaRuta.Cumple(pasajero.Estado, filtro))
            {
                continue;
            }

            var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(pasajero.EmpleadoId);
            informe.Filas.Add(new FilaInformeAsistencia
            {
                Numero = numeroPasajero,
                Cedula = empleado?.Usuario?.Cedula ?? string.Empty,
                NombreCompleto = empleado?.NombreCompleto ?? string.Empty,
                Estado = pasajero.Estado,
                // Los instantes reales se guardan en UTC; el informe se entrega en hora de Colombia.
                HoraRegistro = pasajero.HoraProcesado is null ? null : ReglasHoraColombia.AhoraColombia(pasajero.HoraProcesado.Value),
                EsperaSegundos = pasajero.HoraLlegadaConductor is null || pasajero.HoraProcesado is null
                    ? null
                    : Math.Max(0, (int)(pasajero.HoraProcesado.Value - pasajero.HoraLlegadaConductor.Value).TotalSeconds)
            });
        }

        return new ArchivoSoporte(NombreArchivo(informe), _generador.Generar(informe));
    }

    private async Task<string?> ObtenerNombreConductorAsync(int? unidadOperativaId)
    {
        if (unidadOperativaId is null)
        {
            return null;
        }

        var unidad = await _unidadOperativaRepositorio.ObtenerPorIdAsync(unidadOperativaId.Value);
        var conductor = unidad is null ? null : await _conductorRepositorio.ObtenerPorIdAsync(unidad.ConductorId);
        return string.IsNullOrWhiteSpace(conductor?.NombreCompleto) ? null : conductor.NombreCompleto;
    }

    private static string NombreArchivo(InformeAsistenciaRuta informe)
    {
        var parte = informe.Filtro switch
        {
            FiltroAsistenciaRuta.ASISTIERON => "asistieron",
            FiltroAsistenciaRuta.NO_ASISTIERON => "no-asistieron",
            _ => "pasajeros"
        };
        return $"{parte}-{informe.Fecha:yyyy-MM-dd}-{informe.Hora:HHmm}.xlsx";
    }
}
