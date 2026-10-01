using TransportApp.Application.DTOs.Planificacion;
using TransportApp.Application.Implementations;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;

namespace TransportApp.UnitTests.Application.Implementations;

public class PlanificacionServicioTests
{
    private class ServicioRepositorioFalso : IServicioRepositorio
    {
        public readonly List<Servicio> Servicios = new();

        public Task<Servicio?> ObtenerPorIdAsync(int servicioId)
            => Task.FromResult(Servicios.FirstOrDefault(s => s.ServicioId == servicioId));

        public Task<List<Servicio>> ObtenerPorJornadaAsync(int jornadaId)
            => Task.FromResult(Servicios.Where(s => s.JornadaId == jornadaId).ToList());

        public Task<List<Servicio>> ObtenerPorUnidadOperativaAsync(int unidadOperativaId)
            => Task.FromResult(Servicios.Where(s => s.UnidadOperativaId == unidadOperativaId).ToList());

        public Task<List<Servicio>> ObtenerPublicadosPorTipoAsync(TipoServicio tipo)
            => Task.FromResult(Servicios.Where(s => s.Estado == EstadoServicio.PUBLICADO && s.Tipo == tipo).ToList());


        public Task<List<Servicio>> ObtenerPendientesDeProgramacionAsync(int empresaId, DateOnly desde) => Task.FromResult(new List<Servicio>());



        public Task<List<Servicio>> ObtenerActivosPorFechaYHoraAsync(DateOnly fecha, TimeOnly hora) => Task.FromResult(new List<Servicio>());

        public Task AgregarAsync(Servicio servicio)
        {
            Servicios.Add(servicio);
            return Task.CompletedTask;
        }

        public Task EliminarAsync(Servicio servicio)
        {
            Servicios.Remove(servicio);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class ServicioPasajeroRepositorioFalso : IServicioPasajeroRepositorio
    {
        public readonly List<ServicioPasajero> Pasajeros = new();

        public Task<ServicioPasajero?> ObtenerPorIdAsync(int servicioPasajeroId) => Task.FromResult<ServicioPasajero?>(null);

        public Task<ServicioPasajero?> ObtenerPorProgramacionAsync(int programacionTransporteId) => Task.FromResult<ServicioPasajero?>(null);

        public Task<List<ServicioPasajero>> ObtenerPorServicioAsync(int servicioId)
            => Task.FromResult(Pasajeros.Where(p => p.ServicioId == servicioId).ToList());

        public Task<List<ServicioPasajero>> ObtenerPorEmpleadoAsync(int empleadoId)
            => Task.FromResult(Pasajeros.Where(p => p.EmpleadoId == empleadoId).ToList());

        public Task AgregarAsync(ServicioPasajero servicioPasajero)
        {
            Pasajeros.Add(servicioPasajero);
            return Task.CompletedTask;
        }

        public void Descartar(ServicioPasajero servicioPasajero) { }

        public Task EliminarAsync(ServicioPasajero servicioPasajero)
        {
            Pasajeros.Remove(servicioPasajero);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class SedeRepositorioFalso : ISedeRepositorio
    {
        private readonly Dictionary<int, Sede> _sedes;

        public SedeRepositorioFalso(params Sede[] sedes)
            => _sedes = sedes.ToDictionary(s => s.SedeId);

        public Task<Sede?> ObtenerPorIdAsync(int sedeId)
            => Task.FromResult(_sedes.TryGetValue(sedeId, out var s) ? s : null);

        public Task<List<Sede>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_sedes.Values.Where(s => s.EmpresaId == empresaId).ToList());

        public Task AgregarAsync(Sede sede) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class UnidadOperativaRepositorioFalso : IUnidadOperativaRepositorio
    {
        private readonly Dictionary<int, UnidadOperativa> _unidades;

        public UnidadOperativaRepositorioFalso(params UnidadOperativa[] unidades)
            => _unidades = unidades.ToDictionary(u => u.UnidadOperativaId);

        public Task<UnidadOperativa?> ObtenerPorIdAsync(int unidadOperativaId)
            => Task.FromResult(_unidades.TryGetValue(unidadOperativaId, out var u) ? u : null);

        public Task<UnidadOperativa?> ObtenerPorVehiculoAsync(int vehiculoId) => Task.FromResult<UnidadOperativa?>(null);

        public Task<List<UnidadOperativa>> ObtenerPorConductorAsync(int conductorId) => Task.FromResult(new List<UnidadOperativa>());

        public Task AgregarAsync(UnidadOperativa unidadOperativa) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private class VehiculoRepositorioFalso : IVehiculoRepositorio
    {
        private readonly Dictionary<int, Vehiculo> _vehiculos;

        public VehiculoRepositorioFalso(params Vehiculo[] vehiculos)
            => _vehiculos = vehiculos.ToDictionary(v => v.VehiculoId);

        public Task<Vehiculo?> ObtenerPorIdAsync(int vehiculoId)
            => Task.FromResult(_vehiculos.TryGetValue(vehiculoId, out var v) ? v : null);

        public Task<Vehiculo?> ObtenerPorPlacaAsync(string placa)
            => Task.FromResult(_vehiculos.Values.FirstOrDefault(v => v.Placa == placa));

        public Task<List<Vehiculo>> ObtenerPorConductorAsync(int conductorId) => Task.FromResult(new List<Vehiculo>());

        public Task AgregarAsync(Vehiculo vehiculo) => Task.CompletedTask;

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private const int EmpresaId = 1;
    private const int UnidadOperativaId = 1;
    private const int VehiculoId = 1;

    private static Jornada CrearJornada() => new() { JornadaId = 1, EmpresaId = EmpresaId, FechaOperativa = new DateOnly(2026, 1, 20) };

    private static PlanificacionServicio CrearServicio(
        ServicioRepositorioFalso servicios,
        ServicioPasajeroRepositorioFalso pasajeros,
        SedeRepositorioFalso sedes,
        UnidadOperativaRepositorioFalso? unidades = null,
        VehiculoRepositorioFalso? vehiculos = null,
        OpcionesPlanificacion? opciones = null)
        => new(
            servicios,
            pasajeros,
            sedes,
            unidades ?? new UnidadOperativaRepositorioFalso(),
            vehiculos ?? new VehiculoRepositorioFalso(),
            opciones ?? new OpcionesPlanificacion());

    [Fact]
    public async Task GenerarPropuestaAsync_LanzaExcepcion_CuandoElServicioNoExisteEnLaEmpresa()
    {
        var jornada = CrearJornada();
        var servicioEntidad = new Servicio { ServicioId = 1, JornadaId = jornada.JornadaId, SedeId = 1, Jornada = jornada };
        var servicios = new ServicioRepositorioFalso();
        servicios.Servicios.Add(servicioEntidad);
        var servicio = CrearServicio(servicios, new ServicioPasajeroRepositorioFalso(), new SedeRepositorioFalso());

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.GenerarPropuestaAsync(empresaId: 2, servicioEntidad.ServicioId));
    }

    [Fact]
    public async Task GenerarPropuestaAsync_OrdenaPasajerosDeEntrada_DelMasLejanoAlMasCercanoDeLaSede()
    {
        var jornada = CrearJornada();
        var sede = new Sede { SedeId = 1, EmpresaId = EmpresaId, Latitud = 0, Longitud = 0, Activa = true };
        var servicioEntidad = new Servicio
        {
            ServicioId = 1, JornadaId = jornada.JornadaId, SedeId = 1, Tipo = TipoServicio.ENTRADA,
            HoraProgramada = new TimeOnly(8, 0), Fecha = new DateOnly(2026, 1, 20), Jornada = jornada
        };
        var servicios = new ServicioRepositorioFalso();
        servicios.Servicios.Add(servicioEntidad);
        var pasajeros = new ServicioPasajeroRepositorioFalso();
        pasajeros.Pasajeros.Add(new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, EmpleadoId = 10, Latitud = 1, Longitud = 0 });
        pasajeros.Pasajeros.Add(new ServicioPasajero { ServicioPasajeroId = 2, ServicioId = 1, EmpleadoId = 20, Latitud = 3, Longitud = 0 });
        var servicio = CrearServicio(servicios, pasajeros, new SedeRepositorioFalso(sede));

        var propuesta = await servicio.GenerarPropuestaAsync(EmpresaId, 1);

        Assert.Equal([2, 1], propuesta.OrdenPropuesto.Select(p => p.ServicioPasajeroId));
        Assert.Equal(1, propuesta.OrdenPropuesto[0].Orden);
        Assert.Equal(2, propuesta.OrdenPropuesto[1].Orden);
    }

    [Fact]
    public async Task GenerarPropuestaAsync_ConservaElOrdenExistente_ParaServiciosDeSalida()
    {
        var jornada = CrearJornada();
        var sede = new Sede { SedeId = 1, EmpresaId = EmpresaId, Latitud = 0, Longitud = 0, Activa = true };
        var servicioEntidad = new Servicio
        {
            ServicioId = 1, JornadaId = jornada.JornadaId, SedeId = 1, Tipo = TipoServicio.SALIDA,
            HoraProgramada = new TimeOnly(17, 0), Fecha = new DateOnly(2026, 1, 20), Jornada = jornada
        };
        var servicios = new ServicioRepositorioFalso();
        servicios.Servicios.Add(servicioEntidad);
        var pasajeros = new ServicioPasajeroRepositorioFalso();
        // El más lejano (Latitud 3) tiene Orden menor: si aplicara la heurística de entrada iría primero.
        pasajeros.Pasajeros.Add(new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, EmpleadoId = 10, Latitud = 3, Longitud = 0, Orden = 2 });
        pasajeros.Pasajeros.Add(new ServicioPasajero { ServicioPasajeroId = 2, ServicioId = 1, EmpleadoId = 20, Latitud = 1, Longitud = 0, Orden = 1 });
        var servicio = CrearServicio(servicios, pasajeros, new SedeRepositorioFalso(sede));

        var propuesta = await servicio.GenerarPropuestaAsync(EmpresaId, 1);

        Assert.Equal([2, 1], propuesta.OrdenPropuesto.Select(p => p.ServicioPasajeroId));
        Assert.Null(propuesta.HoraLimiteLlegadaSede);
        Assert.Null(propuesta.HoraSugeridaInicioRecogida);
    }

    [Fact]
    public async Task GenerarPropuestaAsync_Advierte_CuandoLaCantidadDePasajerosExcedeLaCapacidad()
    {
        var jornada = CrearJornada();
        var servicioEntidad = new Servicio
        {
            ServicioId = 1, JornadaId = jornada.JornadaId, SedeId = 1, Tipo = TipoServicio.ENTRADA,
            HoraProgramada = new TimeOnly(8, 0), UnidadOperativaId = UnidadOperativaId, Jornada = jornada
        };
        var servicios = new ServicioRepositorioFalso();
        servicios.Servicios.Add(servicioEntidad);
        var pasajeros = new ServicioPasajeroRepositorioFalso();
        pasajeros.Pasajeros.Add(new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, EmpleadoId = 10 });
        pasajeros.Pasajeros.Add(new ServicioPasajero { ServicioPasajeroId = 2, ServicioId = 1, EmpleadoId = 20 });
        var unidades = new UnidadOperativaRepositorioFalso(new UnidadOperativa { UnidadOperativaId = UnidadOperativaId, VehiculoId = VehiculoId, ConductorId = 1, Activa = true });
        var vehiculos = new VehiculoRepositorioFalso(new Vehiculo { VehiculoId = VehiculoId, Capacidad = 1, ConductorId = 1, Activo = true });
        var servicio = CrearServicio(servicios, pasajeros, new SedeRepositorioFalso(), unidades, vehiculos);

        var propuesta = await servicio.GenerarPropuestaAsync(EmpresaId, 1);

        Assert.Contains(propuesta.Advertencias, a => a.Contains("excede la capacidad"));
    }

    [Fact]
    public async Task GenerarPropuestaAsync_NoAdvierte_CuandoLaCapacidadAlcanza()
    {
        var jornada = CrearJornada();
        var servicioEntidad = new Servicio
        {
            ServicioId = 1, JornadaId = jornada.JornadaId, SedeId = 1, Tipo = TipoServicio.ENTRADA,
            HoraProgramada = new TimeOnly(8, 0), UnidadOperativaId = UnidadOperativaId, Jornada = jornada
        };
        var servicios = new ServicioRepositorioFalso();
        servicios.Servicios.Add(servicioEntidad);
        var pasajeros = new ServicioPasajeroRepositorioFalso();
        pasajeros.Pasajeros.Add(new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, EmpleadoId = 10 });
        var unidades = new UnidadOperativaRepositorioFalso(new UnidadOperativa { UnidadOperativaId = UnidadOperativaId, VehiculoId = VehiculoId, ConductorId = 1, Activa = true });
        var vehiculos = new VehiculoRepositorioFalso(new Vehiculo { VehiculoId = VehiculoId, Capacidad = 4, ConductorId = 1, Activo = true });
        var servicio = CrearServicio(servicios, pasajeros, new SedeRepositorioFalso(), unidades, vehiculos);

        var propuesta = await servicio.GenerarPropuestaAsync(EmpresaId, 1);

        Assert.DoesNotContain(propuesta.Advertencias, a => a.Contains("excede la capacidad"));
    }

    [Fact]
    public async Task GenerarPropuestaAsync_Advierte_CuandoNoHayUnidadAsignada()
    {
        var jornada = CrearJornada();
        var servicioEntidad = new Servicio
        {
            ServicioId = 1, JornadaId = jornada.JornadaId, SedeId = 1, Tipo = TipoServicio.ENTRADA,
            HoraProgramada = new TimeOnly(8, 0), UnidadOperativaId = null, Jornada = jornada
        };
        var servicios = new ServicioRepositorioFalso();
        servicios.Servicios.Add(servicioEntidad);
        var servicio = CrearServicio(servicios, new ServicioPasajeroRepositorioFalso(), new SedeRepositorioFalso());

        var propuesta = await servicio.GenerarPropuestaAsync(EmpresaId, 1);

        Assert.Contains(propuesta.Advertencias, a => a.Contains("no tiene una unidad operativa asignada"));
    }

    [Fact]
    public async Task GenerarPropuestaAsync_CalculaHoraLimiteYHoraSugerida_ParaEntrada()
    {
        var jornada = CrearJornada();
        var servicioEntidad = new Servicio
        {
            ServicioId = 1, JornadaId = jornada.JornadaId, SedeId = 1, Tipo = TipoServicio.ENTRADA,
            HoraProgramada = new TimeOnly(8, 0), Jornada = jornada
        };
        var servicios = new ServicioRepositorioFalso();
        servicios.Servicios.Add(servicioEntidad);
        var opciones = new OpcionesPlanificacion { VentanaRecogidaMinutos = 25 };
        var servicio = CrearServicio(servicios, new ServicioPasajeroRepositorioFalso(), new SedeRepositorioFalso(), opciones: opciones);

        var propuesta = await servicio.GenerarPropuestaAsync(EmpresaId, 1);

        Assert.Equal(new TimeOnly(7, 45), propuesta.HoraLimiteLlegadaSede);
        Assert.Equal(new TimeOnly(7, 20), propuesta.HoraSugeridaInicioRecogida);
    }

    [Fact]
    public async Task GenerarPropuestaAsync_CalculaDistanciaConServicioAnteriorYSiguienteDeLaMismaUnidad()
    {
        var jornada = CrearJornada();
        const int OtraSedeId = 2;
        var sedeActual = new Sede { SedeId = 1, EmpresaId = EmpresaId, Latitud = 0, Longitud = 0, Activa = true };
        var sedeAdyacente = new Sede { SedeId = OtraSedeId, EmpresaId = EmpresaId, Latitud = 5, Longitud = 0, Activa = true };

        var servicioActual = new Servicio
        {
            ServicioId = 1, JornadaId = jornada.JornadaId, SedeId = 1, Tipo = TipoServicio.ENTRADA,
            HoraProgramada = new TimeOnly(8, 0), Fecha = new DateOnly(2026, 1, 20),
            UnidadOperativaId = UnidadOperativaId, Jornada = jornada
        };
        // Anterior: ENTRADA a otra sede, más temprano el mismo día -> termina en esa sede.
        var servicioAnterior = new Servicio
        {
            ServicioId = 2, JornadaId = jornada.JornadaId, SedeId = OtraSedeId, Tipo = TipoServicio.ENTRADA,
            HoraProgramada = new TimeOnly(6, 0), Fecha = new DateOnly(2026, 1, 20),
            UnidadOperativaId = UnidadOperativaId, Jornada = jornada
        };
        // Siguiente: SALIDA desde la sede actual -> primera ubicación es la propia sede.
        var servicioSiguiente = new Servicio
        {
            ServicioId = 3, JornadaId = jornada.JornadaId, SedeId = 1, Tipo = TipoServicio.SALIDA,
            HoraProgramada = new TimeOnly(17, 0), Fecha = new DateOnly(2026, 1, 20),
            UnidadOperativaId = UnidadOperativaId, Jornada = jornada
        };

        var servicios = new ServicioRepositorioFalso();
        servicios.Servicios.AddRange([servicioActual, servicioAnterior, servicioSiguiente]);

        var pasajeros = new ServicioPasajeroRepositorioFalso();
        pasajeros.Pasajeros.Add(new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, EmpleadoId = 10, Latitud = 1, Longitud = 0 });

        var unidades = new UnidadOperativaRepositorioFalso(new UnidadOperativa { UnidadOperativaId = UnidadOperativaId, VehiculoId = VehiculoId, ConductorId = 1, Activa = true });
        var vehiculos = new VehiculoRepositorioFalso(new Vehiculo { VehiculoId = VehiculoId, Capacidad = 4, ConductorId = 1, Activo = true });
        var servicio = CrearServicio(servicios, pasajeros, new SedeRepositorioFalso(sedeActual, sedeAdyacente), unidades, vehiculos);

        var propuesta = await servicio.GenerarPropuestaAsync(EmpresaId, 1);

        // Anterior termina en sedeAdyacente (5,0); el actual (ENTRADA) empieza en su primer pasajero propuesto (1,0) -> distancia 4 grados.
        Assert.NotNull(propuesta.DistanciaDesdeServicioAnteriorKm);
        Assert.True(propuesta.DistanciaDesdeServicioAnteriorKm > 0);

        // El actual (ENTRADA) termina en su propia sede (0,0); el siguiente (SALIDA) empieza en su propia sede (0,0) -> distancia 0.
        Assert.Equal(0, propuesta.DistanciaHaciaServicioSiguienteKm!.Value, 3);
    }
}
