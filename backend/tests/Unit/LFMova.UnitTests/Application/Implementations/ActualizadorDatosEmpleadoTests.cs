using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Implementations.Importaciones;
using LFMova.Domain.Entities;

namespace LFMova.UnitTests.Application.Implementations;

public class ActualizadorDatosEmpleadoTests
{
    private static Empleado Empleado() => new() { NombreCompleto = "Maria Perez", Telefono = "3117924418", Direccion = "Calle 1 # 2-3", Barrio = "Centro" };

    private static Usuario Usuario(bool activada = false) => new() { Cedula = "123", NombreCompleto = "Maria Perez", Telefono = "3117924418", PasswordHash = activada ? "hash" : null };

    private static FilaHoja Fila(string nombre = "Maria Perez", string direccion = "Calle 1 # 2-3", string barrio = "Centro", string celular = "3117924418")
        => new() { Cedula = "123", NombreCompleto = nombre, Direccion = direccion, Barrio = barrio, Celular = celular };

    [Fact]
    public void Aplicar_ActualizaNombreDireccionBarrioYTelefonoYAvisaDeLoQueCambio()
    {
        var empleado = Empleado();
        var resultado = new ResultadoImportacionDto();

        var hubo = ActualizadorDatosEmpleado.Aplicar(empleado, Usuario(), Fila("Maria Jose Perez", "Carrera 9 # 8-7", "Prado", "3227001122"), resultado);

        Assert.True(hubo);
        Assert.Equal(("Maria Jose Perez", "Carrera 9 # 8-7", "Prado", "3227001122"), (empleado.NombreCompleto, empleado.Direccion, empleado.Barrio, empleado.Telefono));
        var aviso = Assert.Single(resultado.Advertencias);
        Assert.Contains("nombre", aviso);
        Assert.Contains("dirección", aviso);
        Assert.Contains("barrio", aviso);
        Assert.Contains("3117924418 → 3227001122", aviso);
    }

    [Fact]
    public void Aplicar_SinCambiosNoHaceNadaNiAvisa()
    {
        var resultado = new ResultadoImportacionDto();

        Assert.False(ActualizadorDatosEmpleado.Aplicar(Empleado(), Usuario(), Fila(), resultado));
        Assert.Empty(resultado.Advertencias);
    }

    [Fact]
    public void Aplicar_UnDatoVacioEnElArchivoNoBorraElQueYaEsta()
    {
        var empleado = Empleado();

        ActualizadorDatosEmpleado.Aplicar(empleado, Usuario(), Fila(nombre: "", direccion: "  ", barrio: "", celular: "0"), new ResultadoImportacionDto());

        Assert.Equal(("Maria Perez", "Calle 1 # 2-3", "Centro", "3117924418"), (empleado.NombreCompleto, empleado.Direccion, empleado.Barrio, empleado.Telefono));
    }

    [Fact]
    public void Aplicar_LlenarUnDatoVacioNoGeneraAvisoYUnaDiferenciaDeMayusculasTampoco()
    {
        var empleado = Empleado();
        empleado.Barrio = string.Empty;
        var resultado = new ResultadoImportacionDto();

        ActualizadorDatosEmpleado.Aplicar(empleado, Usuario(), Fila(nombre: "MARIA PEREZ", barrio: "Prado"), resultado);

        Assert.Equal(("MARIA PEREZ", "Prado"), (empleado.NombreCompleto, empleado.Barrio));
        Assert.Empty(resultado.Advertencias);
    }

    [Fact]
    public void Aplicar_NuncaCambiaUnCelularPorUnFijo()
    {
        var empleado = Empleado();

        ActualizadorDatosEmpleado.Aplicar(empleado, Usuario(), Fila(celular: "8915645"), new ResultadoImportacionDto());

        Assert.Equal("3117924418", empleado.Telefono);
    }

    [Fact]
    public void Aplicar_LaCuentaSoloSeActualizaSiLaPersonaNoLaHaActivado()
    {
        var pendiente = Usuario();
        var activada = Usuario(activada: true);

        ActualizadorDatosEmpleado.Aplicar(Empleado(), pendiente, Fila("Maria Jose Perez", celular: "3227001122"), new ResultadoImportacionDto());
        ActualizadorDatosEmpleado.Aplicar(Empleado(), activada, Fila("Maria Jose Perez", celular: "3227001122"), new ResultadoImportacionDto());

        Assert.Equal(("Maria Jose Perez", "3227001122"), (pendiente.NombreCompleto, pendiente.Telefono));
        Assert.Equal(("Maria Perez", "3117924418"), (activada.NombreCompleto, activada.Telefono));
    }

    [Fact]
    public void Aplicar_LaCedulaNuncaCambia()
    {
        var usuario = Usuario();

        ActualizadorDatosEmpleado.Aplicar(Empleado(), usuario, new FilaHoja { Cedula = "999", NombreCompleto = "Otra", Direccion = "x", Barrio = "y", Celular = "3000000000" }, new ResultadoImportacionDto());

        Assert.Equal("123", usuario.Cedula);
    }
}
