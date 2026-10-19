using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LFMova.Application.DTOs.Autenticacion;
using LFMova.Application.DTOs.Facturacion;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Data;
using LFMova.IntegrationTests.Fixtures;

namespace LFMova.IntegrationTests;

/// <summary>
/// Prueba contra PostgreSQL la facturación del administrador: el conteo de
/// conductores por mes (incluido uno cuya cuenta ya se eliminó), el cierre
/// mensual, el Excel de soporte y que solo el administrador tiene acceso.
/// </summary>
[Collection(IntegrationTestCollection.Nombre)]
public class FacturacionTests
{
    private readonly IntegrationTestFixture _fixture;

    /// <summary>Crea la prueba con la colección compartida de PostgreSQL.</summary>
    public FacturacionTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Administrador_VeElMesLoCierraYDescargaElSoporte_ContandoTambienAlConductorEliminado()
    {
        var empresaId = await SembrarAsync("FAC-A");
        using var administrador = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(administrador, "FAC-A-ADMIN", "ClaveAdmin123");

        // --- Resumen de marzo de 2025: dos conductores (uno ya eliminado), tres rutas, siete pasajeros. ---
        var resumen = await administrador.GetFromJsonAsync<List<ResumenFacturacionEmpresaDto>>("/api/facturacion?anio=2025&mes=3");
        var fila = resumen!.Single(r => r.EmpresaId == empresaId);
        Assert.Equal(1, fila.ConductoresVinculados);
        Assert.Equal(2, fila.ConductoresActivos);
        Assert.Equal(3, fila.RutasFinalizadas);
        Assert.Equal(7, fila.PasajerosTransportados);
        Assert.False(fila.Cerrado);

        var detalle = await administrador.GetFromJsonAsync<DetalleFacturacionDto>($"/api/facturacion/empresas/{empresaId}?anio=2025&mes=3");
        var vigente = detalle!.Conductores.Single(c => c.Cedula == "FAC-A-COND");
        Assert.False(vigente.Eliminado);
        Assert.Equal(2, vigente.RutasFinalizadas);
        Assert.Equal(new DateOnly(2025, 3, 3), vigente.PrimeraRuta);
        Assert.Equal(new DateOnly(2025, 3, 20), vigente.UltimaRuta);
        var eliminado = detalle.Conductores.Single(c => c.Cedula == "FAC-A-BORRADO");
        Assert.True(eliminado.Eliminado);

        // --- Abril: el eliminado ya no aparece. ---
        var abril = await administrador.GetFromJsonAsync<DetalleFacturacionDto>($"/api/facturacion/empresas/{empresaId}?anio=2025&mes=4");
        Assert.Equal("FAC-A-COND", Assert.Single(abril!.Conductores).Cedula);

        // --- Cierre: una vez, y los totales quedan guardados. ---
        var cierre = await administrador.PostAsync($"/api/facturacion/empresas/{empresaId}/cierres?anio=2025&mes=3", null);
        cierre.EnsureSuccessStatusCode();
        Assert.True((await cierre.Content.ReadFromJsonAsync<DetalleFacturacionDto>())!.Resumen.Cerrado);
        var repetido = await administrador.PostAsync($"/api/facturacion/empresas/{empresaId}/cierres?anio=2025&mes=3", null);
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);

        using (var alcance = _fixture.CrearAlcance())
        {
            var guardado = await alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>().CierresMensuales.SingleAsync(c => c.EmpresaId == empresaId);
            Assert.Equal(2, guardado.ConductoresActivos);
            Assert.Equal(3, guardado.RutasFinalizadas);
        }

        // --- Excel de soporte: trae a los dos conductores. ---
        var excel = await administrador.GetAsync($"/api/facturacion/empresas/{empresaId}/excel?anio=2025&mes=3");
        excel.EnsureSuccessStatusCode();
        using var libro = new XLWorkbook(new MemoryStream(await excel.Content.ReadAsByteArrayAsync()));
        var textos = libro.Worksheet(1).CellsUsed().Select(c => c.GetString()).ToList();
        Assert.Contains("FAC-A-COND", textos);
        Assert.Contains("FAC-A-BORRADO", textos);
        Assert.Contains(textos, t => t.StartsWith("Mes cerrado"));
    }

    [Fact]
    public async Task CerrarElMesEnCurso_RespondeConflicto()
    {
        var empresaId = await SembrarAsync("FAC-B");
        using var administrador = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(administrador, "FAC-B-ADMIN", "ClaveAdmin123");
        var hoy = DateTime.UtcNow.AddHours(-5);

        var respuesta = await administrador.PostAsync($"/api/facturacion/empresas/{empresaId}/cierres?anio={hoy.Year}&mes={hoy.Month}", null);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task Coordinador_NoTieneAccesoALaFacturacion()
    {
        await SembrarAsync("FAC-C");
        using var coordinador = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(coordinador, "FAC-C-COORD", "ClaveCoord123");

        var respuesta = await coordinador.GetAsync("/api/facturacion?anio=2025&mes=3");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    /// <summary>
    /// Crea una empresa con un administrador, un coordinador y un conductor
    /// vinculado, y el registro de uso de marzo y abril de 2025: dos rutas del
    /// conductor vigente y una de otro cuya cuenta ya no existe en marzo, y
    /// una del vigente en abril.
    /// </summary>
    private async Task<int> SembrarAsync(string prefijo)
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, $"{prefijo} empresa");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, $"{prefijo}-ADMIN", "ClaveAdmin123", Rol.ADMINISTRADOR_PLATAFORMA, null);
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, $"{prefijo}-COORD", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        var usuario = await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, $"{prefijo}-COND", "ClaveCond123", Rol.CONDUCTOR, null);
        var conductor = new Conductor { UsuarioId = usuario.UsuarioId, NombreCompleto = "Conductor Vigente", Telefono = "300", Activo = true };
        conductor.VinculacionesConductorEmpresa.Add(new VinculacionConductorEmpresa { EmpresaId = empresa.EmpresaId, Activa = true });
        contexto.Conductores.Add(conductor);
        await contexto.SaveChangesAsync();

        UsoConductor Uso(int servicioId, string cedula, string nombre, DateOnly fecha, int pasajeros) => new()
        {
            EmpresaId = empresa.EmpresaId, ServicioId = empresa.EmpresaId * 1000 + servicioId, Fecha = fecha, ConductorId = conductor.ConductorId,
            Cedula = cedula, NombreConductor = nombre, Placa = "FAC001", PasajerosTransportados = pasajeros
        };
        contexto.UsosConductor.AddRange(
            Uso(1, $"{prefijo}-COND", "Conductor Vigente", new DateOnly(2025, 3, 3), 3),
            Uso(2, $"{prefijo}-COND", "Conductor Vigente", new DateOnly(2025, 3, 20), 2),
            Uso(3, $"{prefijo}-BORRADO", "Conductor Eliminado", new DateOnly(2025, 3, 31), 2),
            Uso(4, $"{prefijo}-COND", "Conductor Vigente", new DateOnly(2025, 4, 1), 4));
        await contexto.SaveChangesAsync();

        return empresa.EmpresaId;
    }

    private static async Task AutenticarAsync(HttpClient cliente, string identificador, string password)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/iniciar-sesion", new IniciarSesionDto { Identificador = identificador, Password = password });
        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaAutenticacionDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cuerpo!.Token);
    }
}
