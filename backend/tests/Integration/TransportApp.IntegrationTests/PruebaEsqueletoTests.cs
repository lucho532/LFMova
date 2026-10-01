using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TransportApp.IntegrationTests;

/// <summary>
/// Verifica que la aplicación <c>TransportApp.Api</c> arranca correctamente
/// dentro del host de pruebas de integración. No cubre ninguna regla de negocio:
/// eso corresponde a las pruebas que se agregarán junto con cada funcionalidad
/// implementada (autenticación, endpoints, multiempresa, etc.).
/// </summary>
public class PruebaEsqueletoTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _fabrica;

    public PruebaEsqueletoTests(WebApplicationFactory<Program> fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task LaAplicacionArrancaSinErroresInternos()
    {
        var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/");

        Assert.NotEqual(HttpStatusCode.InternalServerError, respuesta.StatusCode);
    }
}
