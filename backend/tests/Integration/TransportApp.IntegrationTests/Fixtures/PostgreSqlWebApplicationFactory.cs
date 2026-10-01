using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TransportApp.Application.Interfaces;

namespace TransportApp.IntegrationTests.Fixtures;

/// <summary>
/// Levanta <c>TransportApp.Api</c> apuntando a la cadena de conexión de
/// PostgreSQL real proporcionada (contenedor de pruebas, ver
/// <c>tasks.md</c> T114) y con una clave JWT de pruebas, ya que
/// <c>appsettings.json</c> no incluye secretos reales.
/// </summary>
/// <remarks>
/// Usa <see cref="IWebHostBuilder.UseSetting"/> en lugar de
/// <c>ConfigureAppConfiguration</c> con una colección en memoria. Con el
/// modelo de hosting mínimo (<c>Program.cs</c> con top-level statements),
/// <c>AgregarInfraestructura</c> lee la cadena de conexión de forma
/// inmediata (<c>configuracion.GetConnectionString(...)</c>) antes de que
/// <c>Program.Build()</c> termine; una colección en memoria agregada vía
/// <c>ConfigureAppConfiguration</c> se añade demasiado tarde para ese valor
/// ya capturado, por lo que la aplicación seguía usando la cadena de
/// conexión real de <c>appsettings.Development.json</c> del desarrollador en
/// lugar de la del contenedor de pruebas. <c>UseSetting</c> sí se aplica a
/// tiempo porque pasa a formar parte de la configuración inicial del host.
/// </remarks>
public class PostgreSqlWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _cadenaConexion;

    /// <summary>Servicio de correo falso que reemplaza al de Brevo durante las pruebas.</summary>
    public ServicioCorreoFalso Correo { get; } = new();

    /// <summary>Crea la fábrica con la cadena de conexión del PostgreSQL de pruebas.</summary>
    public PostgreSqlWebApplicationFactory(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");
        builder.UseSetting("ConnectionStrings:TransportAppDb", _cadenaConexion);
        builder.UseSetting("Jwt:ClaveSecreta", "clave-secreta-unicamente-para-pruebas-de-integracion-0123456789");
        builder.UseSetting("Jwt:Emisor", "TransportApp");
        builder.UseSetting("Jwt:Audiencia", "TransportApp");
        builder.UseSetting("Jwt:ExpiracionMinutos", "60");
        builder.UseSetting("Frontend:UrlBase", "http://frontend.pruebas");

        builder.ConfigureServices(servicios =>
        {
            servicios.RemoveAll<IServicioCorreo>();
            servicios.AddSingleton<IServicioCorreo>(Correo);
        });
    }
}
