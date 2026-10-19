using LFMova.Application.DTOs.Empleados;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;

namespace LFMova.UnitTests.Application.Implementations;

public class EmpleadoServicioTests
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

    private static Empleado CrearEmpleado(int empleadoId, int empresaId) => new()
    {
        EmpleadoId = empleadoId,
        UsuarioId = empleadoId + 100,
        EmpresaId = empresaId,
        Usuario = new Usuario { UsuarioId = empleadoId + 100, Cedula = $"C{empleadoId}", Activo = true },
        NombreCompleto = "Empleado de Prueba",
        Telefono = "3000000000",
        Direccion = "Calle 1",
        Barrio = "Centro",
        Activo = true
    };

    [Fact]
    public async Task ObtenerPorEmpresaAsync_DevuelveSoloLosEmpleadosDeEsaEmpresa()
    {
        var repositorio = new EmpleadoRepositorioFalso(CrearEmpleado(1, 1), CrearEmpleado(2, 2));
        var servicio = new EmpleadoServicio(repositorio);

        var empleados = await servicio.ObtenerPorEmpresaAsync(1);

        Assert.Single(empleados);
        Assert.Equal(1, empleados[0].EmpleadoId);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_DevuelveNulo_CuandoElEmpleadoEsDeOtraEmpresa()
    {
        var repositorio = new EmpleadoRepositorioFalso(CrearEmpleado(1, 1));
        var servicio = new EmpleadoServicio(repositorio);

        var resultado = await servicio.ObtenerPorIdAsync(2, 1);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ActualizarAsync_ActualizaLosDatosActuales_CuandoPerteneceALaEmpresa()
    {
        var repositorio = new EmpleadoRepositorioFalso(CrearEmpleado(1, 1));
        var servicio = new EmpleadoServicio(repositorio);

        await servicio.ActualizarAsync(1, 1, new ActualizarEmpleadoDto
        {
            NombreCompleto = "Nuevo Nombre",
            Telefono = "3111111111",
            Direccion = "Calle 2",
            Barrio = "Norte"
        });

        var actualizado = await servicio.ObtenerPorIdAsync(1, 1);
        Assert.Equal("Nuevo Nombre", actualizado!.NombreCompleto);
        Assert.Equal("3111111111", actualizado.Telefono);
    }

    [Fact]
    public async Task ActualizarAsync_NuncaCambiaLaEmpresaDelEmpleado()
    {
        // ActualizarEmpleadoDto no expone EmpresaId: el cambio de empresa de
        // un empleado solo puede ocurrir mediante la importación de Excel
        // (T056A), nunca mediante esta operación manual del coordinador.
        var repositorio = new EmpleadoRepositorioFalso(CrearEmpleado(1, 1));
        var servicio = new EmpleadoServicio(repositorio);

        await servicio.ActualizarAsync(1, 1, new ActualizarEmpleadoDto
        {
            NombreCompleto = "Nuevo Nombre",
            Telefono = "3111111111",
            Direccion = "Calle 2",
            Barrio = "Norte"
        });

        var actualizado = await servicio.ObtenerPorIdAsync(1, 1);
        Assert.Equal(1, actualizado!.EmpresaId);
    }

    [Fact]
    public async Task ActualizarAsync_LanzaExcepcion_CuandoElEmpleadoEsDeOtraEmpresa()
    {
        var repositorio = new EmpleadoRepositorioFalso(CrearEmpleado(1, 1));
        var servicio = new EmpleadoServicio(repositorio);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.ActualizarAsync(2, 1, new ActualizarEmpleadoDto { NombreCompleto = "X", Telefono = "X", Direccion = "X", Barrio = "X" }));
    }

    [Fact]
    public async Task ActualizarAsync_LanzaExcepcion_CuandoElTelefonoEstaVacio()
    {
        var repositorio = new EmpleadoRepositorioFalso(CrearEmpleado(1, 1));
        var servicio = new EmpleadoServicio(repositorio);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.ActualizarAsync(1, 1, new ActualizarEmpleadoDto { NombreCompleto = "X", Telefono = "", Direccion = "X", Barrio = "X" }));
    }
}
