using LFMova.Application.DTOs.Soportes;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Pruebas del informe de asistencia por período (una jornada o varios días): qué pasajeros incluye cada
/// filtro, el orden por ruta, los datos de ruta y conductor, la hora de Colombia y la validación del rango.
/// </summary>
public class InformeAsistenciaPeriodoServicioTests
{
    private const int EmpresaId = 1;
    private static readonly DateOnly Dia = new(2026, 10, 15);

    private class RepositorioFalso(params PasajeroDeRutaDto[] pasajeros) : IInformeAsistenciaRepositorio
    {
        public (int EmpresaId, DateOnly Desde, DateOnly Hasta)? Consulta { get; private set; }

        public Task<List<PasajeroDeRutaDto>> ObtenerPasajerosAsync(int empresaId, DateOnly desde, DateOnly hasta)
        {
            Consulta = (empresaId, desde, hasta);
            return Task.FromResult(pasajeros.ToList());
        }
    }

    private class EmpresaRepositorioFalso : IEmpresaRepositorio
    {
        public Task<Empresa?> ObtenerPorIdAsync(int empresaId) => Task.FromResult<Empresa?>(new Empresa { EmpresaId = empresaId, Nombre = "Panamericana" });
        public Task<Empresa?> ObtenerPorCifAsync(string cif) => Task.FromResult<Empresa?>(null);
        public Task<List<Empresa>> ObtenerTodasAsync() => Task.FromResult(new List<Empresa>());
        public Task AgregarAsync(Empresa empresa) => Task.CompletedTask;
        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class GeneradorCapturador : IGeneradorInformeAsistenciaPeriodo
    {
        public InformeAsistenciaPeriodo? Informe { get; private set; }

        public byte[] Generar(InformeAsistenciaPeriodo informe)
        {
            Informe = informe;
            return new byte[] { 1 };
        }
    }

    private static PasajeroDeRutaDto Pasajero(string cedula, EstadoServicioPasajero estado, int servicioId, TimeOnly horaRuta, DateOnly? fecha = null,
        DateTime? llegada = null, DateTime? procesado = null, int orden = 1) => new()
    {
        Fecha = fecha ?? Dia, HoraRuta = horaRuta, ServicioId = servicioId, JornadaId = 5, Tipo = TipoServicio.ENTRADA, Sede = "La Patria",
        Conductor = "Andre Otalvaro", Placa = "STT123", Cedula = cedula, NombreCompleto = $"Pasajero {cedula}", Telefono = "300",
        Direccion = "Calle 1", Barrio = "Centro", Estado = estado, Orden = orden, HoraLlegadaConductor = llegada, HoraProcesado = procesado
    };

    private static (InformeAsistenciaPeriodoServicio servicio, RepositorioFalso repositorio, GeneradorCapturador generador) Crear(params PasajeroDeRutaDto[] pasajeros)
    {
        var repositorio = new RepositorioFalso(pasajeros);
        var generador = new GeneradorCapturador();
        return (new InformeAsistenciaPeriodoServicio(repositorio, new EmpresaRepositorioFalso(), generador), repositorio, generador);
    }

    private static readonly TimeOnly Seis = new(6, 0);
    private static readonly TimeOnly Ocho = new(8, 0);

    [Fact]
    public async Task GenerarAsync_NoAsistieron_IncluyeSoloNoRecogidosYQuienesAvisaron_DeTodasLasRutas()
    {
        var (servicio, _, generador) = Crear(
            Pasajero("1", EstadoServicioPasajero.RECOGIDO, 10, Seis),
            Pasajero("2", EstadoServicioPasajero.NO_RECOGIDO, 10, Seis),
            Pasajero("3", EstadoServicioPasajero.NO_ASISTIRA, 11, Ocho),
            Pasajero("4", EstadoServicioPasajero.PROGRAMADO, 11, Ocho),
            Pasajero("5", EstadoServicioPasajero.CANCELADO, 11, Ocho));

        await servicio.GenerarAsync(EmpresaId, Dia, Dia, FiltroAsistenciaRuta.NO_ASISTIERON);

        Assert.Equal(new[] { "2", "3" }, generador.Informe!.Filas.Select(f => f.Cedula));
    }

    [Fact]
    public async Task GenerarAsync_ConsultaLaEmpresaYElRangoIndicados()
    {
        var (servicio, repositorio, _) = Crear();

        await servicio.GenerarAsync(EmpresaId, Dia, Dia.AddDays(2), FiltroAsistenciaRuta.NO_ASISTIERON);

        Assert.Equal((EmpresaId, Dia, Dia.AddDays(2)), repositorio.Consulta);
    }

    [Fact]
    public async Task GenerarAsync_TraeLosDatosDeLaRutaYDelConductorAsignado()
    {
        var (servicio, _, generador) = Crear(Pasajero("2", EstadoServicioPasajero.NO_RECOGIDO, 10, Seis));

        await servicio.GenerarAsync(EmpresaId, Dia, Dia, FiltroAsistenciaRuta.NO_ASISTIERON);

        var fila = generador.Informe!.Filas.Single();
        Assert.Equal(Dia, fila.Fecha);
        // Con la ruta y la jornada se abre la ficha completa del pasajero desde la vista previa.
        Assert.Equal((10, 5), (fila.ServicioId, fila.JornadaId));
        Assert.Equal(Seis, fila.HoraRuta);
        Assert.Equal("La Patria", fila.Sede);
        Assert.Equal("Andre Otalvaro", fila.Conductor);
        Assert.Equal("STT123", fila.Placa);
        Assert.Equal("Pasajero 2", fila.NombreCompleto);
        Assert.Equal("Calle 1", fila.Direccion);
        Assert.Equal("Panamericana", generador.Informe.NombreEmpresa);
    }

    [Fact]
    public async Task GenerarAsync_OrdenaPorFechaYHoraDeRuta_YDentroDeCadaRutaPorOrdenDeAtencion()
    {
        var (servicio, _, generador) = Crear(
            Pasajero("tarde", EstadoServicioPasajero.NO_RECOGIDO, 11, Ocho, procesado: new DateTime(2026, 10, 15, 13, 0, 0)),
            Pasajero("sinHora", EstadoServicioPasajero.NO_ASISTIRA, 10, Seis, orden: 1),
            Pasajero("segundo", EstadoServicioPasajero.NO_RECOGIDO, 10, Seis, procesado: new DateTime(2026, 10, 15, 11, 30, 0)),
            Pasajero("primero", EstadoServicioPasajero.NO_RECOGIDO, 10, Seis, procesado: new DateTime(2026, 10, 15, 11, 10, 0)),
            Pasajero("diaSiguiente", EstadoServicioPasajero.NO_RECOGIDO, 12, Seis, fecha: Dia.AddDays(1)));

        await servicio.GenerarAsync(EmpresaId, Dia, Dia.AddDays(1), FiltroAsistenciaRuta.NO_ASISTIERON);

        Assert.Equal(new[] { "primero", "segundo", "sinHora", "tarde", "diaSiguiente" }, generador.Informe!.Filas.Select(f => f.Cedula));
    }

    [Fact]
    public async Task GenerarAsync_ConvierteLaHoraAHoraDeColombia_YCalculaLaEspera()
    {
        var (servicio, _, generador) = Crear(Pasajero("2", EstadoServicioPasajero.NO_RECOGIDO, 10, Seis,
            llegada: new DateTime(2026, 10, 15, 11, 0, 0), procesado: new DateTime(2026, 10, 15, 11, 2, 44)));

        await servicio.GenerarAsync(EmpresaId, Dia, Dia, FiltroAsistenciaRuta.NO_ASISTIERON);

        var fila = generador.Informe!.Filas.Single();
        Assert.Equal(new DateTime(2026, 10, 15, 6, 2, 44), fila.HoraRegistro);
        Assert.Equal(164, fila.EsperaSegundos);
    }

    [Theory]
    [InlineData(FiltroAsistenciaRuta.NO_ASISTIERON, "2026-10-15", "2026-10-15", "no-asistieron-2026-10-15.xlsx")]
    [InlineData(FiltroAsistenciaRuta.ASISTIERON, "2026-10-15", "2026-10-17", "asistieron-2026-10-15_a_2026-10-17.xlsx")]
    [InlineData(FiltroAsistenciaRuta.TODOS, "2026-10-15", "2026-10-15", "pasajeros-2026-10-15.xlsx")]
    public async Task GenerarAsync_NombraElArchivoSegunElFiltroYElRango(FiltroAsistenciaRuta filtro, string desde, string hasta, string esperado)
    {
        var (servicio, _, _) = Crear();

        var archivo = await servicio.GenerarAsync(EmpresaId, DateOnly.Parse(desde), DateOnly.Parse(hasta), filtro);

        Assert.Equal(esperado, archivo.NombreArchivo);
    }

    [Fact]
    public async Task ObtenerAsync_DevuelveLosMismosPasajerosQueElExcel_SinGenerarArchivo()
    {
        var (servicio, _, generador) = Crear(
            Pasajero("1", EstadoServicioPasajero.RECOGIDO, 10, Seis),
            Pasajero("2", EstadoServicioPasajero.NO_RECOGIDO, 10, Seis));

        var informe = await servicio.ObtenerAsync(EmpresaId, Dia, Dia, FiltroAsistenciaRuta.NO_ASISTIERON);

        Assert.Equal("2", Assert.Single(informe.Filas).Cedula);
        Assert.Equal(FiltroAsistenciaRuta.NO_ASISTIERON, informe.Filtro);
        Assert.Equal((Dia, Dia), (informe.Desde, informe.Hasta));
        Assert.Null(generador.Informe);
    }

    [Fact]
    public async Task ObtenerAsync_LanzaArgumentException_CuandoLaFechaFinalEsAnteriorALaInicial()
    {
        var (servicio, repositorio, _) = Crear();

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.ObtenerAsync(EmpresaId, Dia, Dia.AddDays(-1), FiltroAsistenciaRuta.NO_ASISTIERON));

        Assert.Null(repositorio.Consulta);
    }

    [Fact]
    public async Task GenerarAsync_LanzaArgumentException_CuandoLaFechaFinalEsAnteriorALaInicial()
    {
        var (servicio, repositorio, generador) = Crear();

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.GenerarAsync(EmpresaId, Dia, Dia.AddDays(-1), FiltroAsistenciaRuta.NO_ASISTIERON));

        Assert.Null(repositorio.Consulta);
        Assert.Null(generador.Informe);
    }
}
