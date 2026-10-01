using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TransportApp.Application.DTOs.Autenticacion;
using TransportApp.Application.DTOs.Zonas;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;
using TransportApp.Infrastructure.Data;
using TransportApp.IntegrationTests.Fixtures;

namespace TransportApp.IntegrationTests;

/// <summary>
/// Prueba el flujo HTTP de macrozonas y barreras geográficas: crear una
/// barrera entre dos barrios y verificar que el sistema impide crear (o
/// actualizar) una zona que junte esos mismos dos barrios, y que una
/// macrozona puede asociarse a una zona.
/// </summary>
[Collection(IntegrationTestCollection.Nombre)]
public class ZonasBarrerasGeograficasTests
{
    private readonly IntegrationTestFixture _fixture;

    /// <summary>Crea la prueba con la colección compartida de PostgreSQL.</summary>
    public ZonasBarrerasGeograficasTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CrearZona_FallaConConflicto_CuandoLosBarriosTienenUnaBarreraGeograficaDeclarada()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<TransportAppDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Barreras");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "BG-COORD", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "BG-COORD", "ClaveCoord123");

        var respuestaBarrera = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/barreras-geograficas", new CrearBarreraGeograficaDto
        {
            BarrioA = "La Linda",
            BarrioB = "La Francia",
            Motivo = "Separados por una ladera sin vía de conexión útil"
        });
        respuestaBarrera.EnsureSuccessStatusCode();

        var respuestaZonaConflicto = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto
        {
            Nombre = "Atardeceres",
            Barrios = new List<string> { "La Linda", "La Francia" }
        });
        Assert.Equal(HttpStatusCode.Conflict, respuestaZonaConflicto.StatusCode);

        var respuestaZonaValida = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto
        {
            Nombre = "Atardeceres Occidente",
            Barrios = new List<string> { "La Linda", "Sacatín" }
        });
        respuestaZonaValida.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CrearMacroZonaYAsociarlaAUnaZona_QuedaReflejadaEnElDto()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<TransportAppDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa MacroZonas");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "MZ-COORD", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "MZ-COORD", "ClaveCoord123");

        var respuestaMacroZona = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/macro-zonas", new CrearMacroZonaDto { Nombre = "Atardeceres" });
        respuestaMacroZona.EnsureSuccessStatusCode();
        var macroZona = (await respuestaMacroZona.Content.ReadFromJsonAsync<MacroZonaDto>())!;

        var respuestaZona = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto
        {
            Nombre = "AT-S01 Occidente",
            Barrios = new List<string> { "Sacatín" },
            MacroZonaId = macroZona.MacroZonaId
        });
        respuestaZona.EnsureSuccessStatusCode();
        var zona = (await respuestaZona.Content.ReadFromJsonAsync<ZonaDto>())!;

        Assert.Equal(macroZona.MacroZonaId, zona.MacroZonaId);
        Assert.Equal("Atardeceres", zona.MacroZonaNombre);
    }

    private async Task AutenticarAsync(HttpClient cliente, string identificador, string password)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/iniciar-sesion", new IniciarSesionDto { Identificador = identificador, Password = password });
        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaAutenticacionDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cuerpo!.Token);
    }
}
