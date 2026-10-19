using LFMova.Application.DTOs.Facturacion;
using LFMova.Application.Implementations;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Pruebas de las reglas de <see cref="FacturacionServicio"/>: qué se cuenta
/// en un mes y cuándo se puede cerrar. Las consultas reales contra la base se
/// prueban en las pruebas de integración.
/// </summary>
public class FacturacionServicioTests
{
    private const int EmpresaId = 1;

    private class GeneradorFalso : IGeneradorCierreMensual
    {
        public DetalleFacturacionDto? Recibido;

        public byte[] Generar(DetalleFacturacionDto detalle)
        {
            Recibido = detalle;
            return new byte[] { 1, 2, 3 };
        }
    }

    private static (FacturacionServicio Servicio, FacturacionRepositorioEnMemoria Repositorio, GeneradorFalso Generador) Crear()
    {
        var repositorio = new FacturacionRepositorioEnMemoria();
        repositorio.Empresas.Add((EmpresaId, "Transportes Uno"));
        repositorio.Empresas.Add((2, "Transportes Dos"));
        var generador = new GeneradorFalso();
        return (new FacturacionServicio(repositorio, generador), repositorio, generador);
    }

    private static UsoConductor Uso(int servicioId, string cedula, DateOnly fecha, int empresaId = EmpresaId, int pasajeros = 3) => new()
    {
        EmpresaId = empresaId, ServicioId = servicioId, Fecha = fecha, Cedula = cedula, NombreConductor = $"Conductor {cedula}", PasajerosTransportados = pasajeros
    };

    [Fact]
    public async Task ObtenerResumenAsync_CuentaConductoresDistintosYSoloLasRutasDelMes()
    {
        var (servicio, repositorio, _) = Crear();
        repositorio.Usos.Add(Uso(1, "100", new DateOnly(2026, 9, 1)));
        repositorio.Usos.Add(Uso(2, "100", new DateOnly(2026, 9, 15)));
        repositorio.Usos.Add(Uso(3, "200", new DateOnly(2026, 9, 30)));
        // Fuera del mes y de otra empresa: no cuentan para la empresa 1 en septiembre.
        repositorio.Usos.Add(Uso(4, "300", new DateOnly(2026, 10, 1)));
        repositorio.Usos.Add(Uso(5, "400", new DateOnly(2026, 9, 10), empresaId: 2));

        var resumen = await servicio.ObtenerResumenAsync(2026, 9);

        var uno = resumen.Single(r => r.EmpresaId == EmpresaId);
        Assert.Equal(2, uno.ConductoresActivos);
        Assert.Equal(3, uno.RutasFinalizadas);
        Assert.Equal(9, uno.PasajerosTransportados);
        Assert.False(uno.Cerrado);
        Assert.Equal(1, resumen.Single(r => r.EmpresaId == 2).ConductoresActivos);
    }

    [Theory]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    [InlineData(1999, 5)]
    public async Task ObtenerResumenAsync_LanzaExcepcion_CuandoElMesNoEsValido(int anio, int mes)
    {
        var (servicio, _, _) = Crear();

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ObtenerResumenAsync(anio, mes));
    }

    [Fact]
    public async Task CerrarMesAsync_GuardaLosTotalesYQuedanFijosAunqueDespuesCambieElRegistro()
    {
        var (servicio, repositorio, _) = Crear();
        repositorio.Usos.Add(Uso(1, "100", new DateOnly(2025, 3, 5)));
        repositorio.Usos.Add(Uso(2, "200", new DateOnly(2025, 3, 6)));

        var cerrado = await servicio.CerrarMesAsync(EmpresaId, 2025, 3);
        // Un registro que apareciera después no altera lo ya cerrado.
        repositorio.Usos.Add(Uso(3, "300", new DateOnly(2025, 3, 7)));
        var despues = (await servicio.ObtenerResumenAsync(2025, 3)).Single(r => r.EmpresaId == EmpresaId);

        Assert.True(cerrado.Resumen.Cerrado);
        Assert.Equal(2, despues.ConductoresActivos);
        Assert.Equal(2, despues.RutasFinalizadas);
        Assert.NotNull(despues.FechaCierre);
        Assert.Single(repositorio.Cierres);
    }

    [Fact]
    public async Task CerrarMesAsync_LanzaExcepcion_CuandoElMesTodaviaNoHaTerminado()
    {
        var (servicio, repositorio, _) = Crear();
        var hoy = DateTime.UtcNow.AddHours(-5);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CerrarMesAsync(EmpresaId, hoy.Year, hoy.Month));

        Assert.Empty(repositorio.Cierres);
    }

    [Fact]
    public async Task CerrarMesAsync_LanzaExcepcion_CuandoElMesYaEstabaCerrado()
    {
        var (servicio, repositorio, _) = Crear();
        await servicio.CerrarMesAsync(EmpresaId, 2025, 3);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CerrarMesAsync(EmpresaId, 2025, 3));

        Assert.Single(repositorio.Cierres);
    }

    [Fact]
    public async Task ObtenerDetalleAsync_LanzaExcepcion_CuandoLaEmpresaNoExiste()
    {
        var (servicio, _, _) = Crear();

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ObtenerDetalleAsync(99, 2026, 9));
    }

    [Fact]
    public async Task DescargarAsync_GeneraElExcelConElDetalleDelMes()
    {
        var (servicio, repositorio, generador) = Crear();
        repositorio.Usos.Add(Uso(1, "100", new DateOnly(2026, 9, 1)));

        var archivo = await servicio.DescargarAsync(EmpresaId, 2026, 9);

        Assert.Equal("facturacion-2026-09-empresa-1.xlsx", archivo.NombreArchivo);
        Assert.Equal(3, archivo.Contenido.Length);
        Assert.Equal("100", Assert.Single(generador.Recibido!.Conductores).Cedula);
    }
}
