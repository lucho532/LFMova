using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using LFMova.Infrastructure.Data;

namespace LFMova.IntegrationTests.Fixtures;

/// <summary>
/// Levanta un PostgreSQL real en un contenedor Docker (Testcontainers, ver
/// <c>tasks.md</c> T114) compartido para toda la colección de pruebas de
/// integración, aplica las migraciones reales de EF Core sobre una base
/// limpia (ver <c>tasks.md</c> T115) y expone una <c>WebApplicationFactory</c>
/// configurada para usarlo.
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    private PostgreSqlContainer _contenedor = null!;

    /// <summary>Fábrica de la aplicación, ya migrada y lista para recibir peticiones HTTP.</summary>
    public PostgreSqlWebApplicationFactory Fabrica { get; private set; } = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _contenedor = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("lfmova_integracion")
            .WithUsername("lfmova")
            .WithPassword("lfmova")
            .Build();

        await _contenedor.StartAsync();

        Fabrica = new PostgreSqlWebApplicationFactory(_contenedor.GetConnectionString());

        using var alcance = Fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        await contexto.Database.MigrateAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        Fabrica.Dispose();
        await _contenedor.DisposeAsync();
    }

    /// <summary>Crea un nuevo alcance de inyección de dependencias sobre la fábrica de pruebas.</summary>
    public IServiceScope CrearAlcance() => Fabrica.Services.CreateScope();
}
