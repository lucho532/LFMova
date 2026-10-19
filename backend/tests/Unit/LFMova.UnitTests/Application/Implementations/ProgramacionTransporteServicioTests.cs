using LFMova.Application.DTOs.Programaciones;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

public class ProgramacionTransporteServicioTests
{
    private class EmpleadoRepositorioFalso : IEmpleadoRepositorio
    {
        private readonly Dictionary<int, Empleado> _empleados;

        public EmpleadoRepositorioFalso(params Empleado[] empleados)
            => _empleados = empleados.ToDictionary(e => e.EmpleadoId);

        public Task<Empleado?> ObtenerPorIdAsync(int empleadoId)
            => Task.FromResult(_empleados.TryGetValue(empleadoId, out var e) ? e : null);

        public Task<Empleado?> ObtenerPorUsuarioIdAsync(int usuarioId)
            => Task.FromResult(_empleados.Values.FirstOrDefault(e => e.UsuarioId == usuarioId));

        public Task AgregarAsync(Empleado empleado)
        {
            _empleados[empleado.EmpleadoId] = empleado;
            return Task.CompletedTask;
        }

        public void Descartar(Empleado empleado) { }

        public Task<List<Empleado>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_empleados.Values.Where(e => e.EmpresaId == empresaId).ToList());

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

    private class ProgramacionRepositorioFalso : IProgramacionTransporteRepositorio
    {
        private readonly Dictionary<int, ProgramacionTransporte> _programaciones = new();
        private int _siguienteId = 1;

        public Task<ProgramacionTransporte?> ObtenerPorIdAsync(int programacionTransporteId)
            => Task.FromResult(_programaciones.TryGetValue(programacionTransporteId, out var p) ? p : null);

        public Task<List<ProgramacionTransporte>> ObtenerPorEmpresaAsync(int empresaId)
            => Task.FromResult(_programaciones.Values.Where(p => p.EmpresaId == empresaId).ToList());

        public Task<ProgramacionTransporte?> ObtenerPorClaveAsync(int empleadoId, int sedeId, DateOnly fecha, TimeOnly hora, TipoServicio tipo)
            => Task.FromResult(_programaciones.Values.FirstOrDefault(p =>
                p.EmpleadoId == empleadoId && p.SedeId == sedeId && p.Fecha == fecha && p.Hora == hora && p.Tipo == tipo));

        public Task AgregarAsync(ProgramacionTransporte programacion)
        {
            programacion.ProgramacionTransporteId = _siguienteId++;
            _programaciones[programacion.ProgramacionTransporteId] = programacion;
            return Task.CompletedTask;
        }

        public void Descartar(ProgramacionTransporte programacion) => _programaciones.Remove(programacion.ProgramacionTransporteId);

        public Task EliminarAsync(ProgramacionTransporte programacion)
        {
            _programaciones.Remove(programacion.ProgramacionTransporteId);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private static CrearProgramacionDto DtoValido(int empleadoId = 1, int sedeId = 1) => new()
    {
        EmpleadoId = empleadoId,
        SedeId = sedeId,
        Fecha = new DateOnly(2026, 1, 15),
        Hora = new TimeOnly(7, 30),
        Tipo = TipoServicio.ENTRADA,
        DireccionRecogida = "Calle 10 # 5-20",
        BarrioRecogida = "Centro"
    };

    [Fact]
    public async Task CrearAsync_Crea_CuandoEmpleadoYSedePertenecenALaEmpresa()
    {
        var empleadoRepo = new EmpleadoRepositorioFalso(new Empleado { EmpleadoId = 1, EmpresaId = 1, Activo = true });
        var sedeRepo = new SedeRepositorioFalso(new Sede { SedeId = 1, EmpresaId = 1, Activa = true });
        var servicio = new ProgramacionTransporteServicio(new ProgramacionRepositorioFalso(), empleadoRepo, sedeRepo);

        var programacion = await servicio.CrearAsync(1, DtoValido());

        Assert.Equal(1, programacion.EmpresaId);
        Assert.Equal(TipoServicio.ENTRADA, programacion.Tipo);
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoElEmpleadoEsDeOtraEmpresa()
    {
        var empleadoRepo = new EmpleadoRepositorioFalso(new Empleado { EmpleadoId = 1, EmpresaId = 2, Activo = true });
        var sedeRepo = new SedeRepositorioFalso(new Sede { SedeId = 1, EmpresaId = 1, Activa = true });
        var servicio = new ProgramacionTransporteServicio(new ProgramacionRepositorioFalso(), empleadoRepo, sedeRepo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearAsync(1, DtoValido()));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoLaSedeEsDeOtraEmpresa()
    {
        var empleadoRepo = new EmpleadoRepositorioFalso(new Empleado { EmpleadoId = 1, EmpresaId = 1, Activo = true });
        var sedeRepo = new SedeRepositorioFalso(new Sede { SedeId = 1, EmpresaId = 2, Activa = true });
        var servicio = new ProgramacionTransporteServicio(new ProgramacionRepositorioFalso(), empleadoRepo, sedeRepo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearAsync(1, DtoValido()));
    }

    [Theory]
    [InlineData("", "Centro")]
    [InlineData("Calle 10", "")]
    public async Task CrearAsync_LanzaExcepcion_CuandoFaltaDireccionOBarrio(string direccion, string barrio)
    {
        var empleadoRepo = new EmpleadoRepositorioFalso(new Empleado { EmpleadoId = 1, EmpresaId = 1, Activo = true });
        var sedeRepo = new SedeRepositorioFalso(new Sede { SedeId = 1, EmpresaId = 1, Activa = true });
        var servicio = new ProgramacionTransporteServicio(new ProgramacionRepositorioFalso(), empleadoRepo, sedeRepo);

        var datos = DtoValido();
        datos.DireccionRecogida = direccion;
        datos.BarrioRecogida = barrio;

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearAsync(1, datos));
    }

    [Fact]
    public async Task CrearAsync_LanzaExcepcion_CuandoLaFechaNoSeEstablece()
    {
        var empleadoRepo = new EmpleadoRepositorioFalso(new Empleado { EmpleadoId = 1, EmpresaId = 1, Activo = true });
        var sedeRepo = new SedeRepositorioFalso(new Sede { SedeId = 1, EmpresaId = 1, Activa = true });
        var servicio = new ProgramacionTransporteServicio(new ProgramacionRepositorioFalso(), empleadoRepo, sedeRepo);

        var datos = DtoValido();
        datos.Fecha = default;

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearAsync(1, datos));
    }

    [Fact]
    public async Task CrearAsync_AceptaTipoSalida_ConDireccionYBarrioObligatorios()
    {
        var empleadoRepo = new EmpleadoRepositorioFalso(new Empleado { EmpleadoId = 1, EmpresaId = 1, Activo = true });
        var sedeRepo = new SedeRepositorioFalso(new Sede { SedeId = 1, EmpresaId = 1, Activa = true });
        var servicio = new ProgramacionTransporteServicio(new ProgramacionRepositorioFalso(), empleadoRepo, sedeRepo);

        var datos = DtoValido();
        datos.Tipo = TipoServicio.SALIDA;

        var programacion = await servicio.CrearAsync(1, datos);

        Assert.Equal(TipoServicio.SALIDA, programacion.Tipo);
        Assert.NotEmpty(programacion.DireccionRecogida);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_DevuelveNulo_CuandoLaProgramacionEsDeOtraEmpresa()
    {
        var empleadoRepo = new EmpleadoRepositorioFalso(new Empleado { EmpleadoId = 1, EmpresaId = 1, Activo = true });
        var sedeRepo = new SedeRepositorioFalso(new Sede { SedeId = 1, EmpresaId = 1, Activa = true });
        var servicio = new ProgramacionTransporteServicio(new ProgramacionRepositorioFalso(), empleadoRepo, sedeRepo);
        var programacion = await servicio.CrearAsync(1, DtoValido());

        var resultado = await servicio.ObtenerPorIdAsync(2, programacion.ProgramacionTransporteId);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ActualizarAsync_ActualizaLosDatos_CuandoPerteneceALaEmpresa()
    {
        var empleadoRepo = new EmpleadoRepositorioFalso(new Empleado { EmpleadoId = 1, EmpresaId = 1, Activo = true });
        var sedeRepo = new SedeRepositorioFalso(
            new Sede { SedeId = 1, EmpresaId = 1, Activa = true },
            new Sede { SedeId = 2, EmpresaId = 1, Activa = true });
        var servicio = new ProgramacionTransporteServicio(new ProgramacionRepositorioFalso(), empleadoRepo, sedeRepo);
        var programacion = await servicio.CrearAsync(1, DtoValido());

        await servicio.ActualizarAsync(1, programacion.ProgramacionTransporteId, new ActualizarProgramacionDto
        {
            SedeId = 2,
            Fecha = new DateOnly(2026, 2, 1),
            Hora = new TimeOnly(8, 0),
            Tipo = TipoServicio.SALIDA,
            DireccionRecogida = "Nueva dirección",
            BarrioRecogida = "Nuevo barrio"
        });

        var actualizada = await servicio.ObtenerPorIdAsync(1, programacion.ProgramacionTransporteId);
        Assert.Equal(2, actualizada!.SedeId);
        Assert.Equal(TipoServicio.SALIDA, actualizada.Tipo);
    }

    [Fact]
    public async Task ActualizarAsync_LanzaExcepcion_CuandoLaNuevaSedeEsDeOtraEmpresa()
    {
        var empleadoRepo = new EmpleadoRepositorioFalso(new Empleado { EmpleadoId = 1, EmpresaId = 1, Activo = true });
        var sedeRepo = new SedeRepositorioFalso(
            new Sede { SedeId = 1, EmpresaId = 1, Activa = true },
            new Sede { SedeId = 2, EmpresaId = 2, Activa = true });
        var servicio = new ProgramacionTransporteServicio(new ProgramacionRepositorioFalso(), empleadoRepo, sedeRepo);
        var programacion = await servicio.CrearAsync(1, DtoValido());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.ActualizarAsync(1, programacion.ProgramacionTransporteId, new ActualizarProgramacionDto
            {
                SedeId = 2,
                Fecha = new DateOnly(2026, 2, 1),
                Hora = new TimeOnly(8, 0),
                Tipo = TipoServicio.SALIDA,
                DireccionRecogida = "X",
                BarrioRecogida = "X"
            }));
    }
}
