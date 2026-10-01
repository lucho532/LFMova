using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TransportApp.Infrastructure.Data;
using TransportApp.IntegrationTests.Fixtures;

namespace TransportApp.IntegrationTests;

/// <summary>
/// Verifica que la base de datos de pruebas se creó correctamente aplicando
/// las migraciones reales de EF Core sobre un PostgreSQL limpio (ver
/// <c>tasks.md</c> T115). La aplicación de migraciones ocurre en
/// <see cref="IntegrationTestFixture.InitializeAsync"/>; aquí solo se
/// verifica el resultado.
/// </summary>
[Collection(IntegrationTestCollection.Nombre)]
public class MigracionesTests
{
    private readonly IntegrationTestFixture _fixture;

    /// <summary>Crea la prueba con la colección compartida de PostgreSQL.</summary>
    public MigracionesTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LasMigracionesSeAplicanSobreUnaBaseLimpia()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<TransportAppDbContext>();

        var migracionesPendientes = await contexto.Database.GetPendingMigrationsAsync();
        var migracionesAplicadas = await contexto.Database.GetAppliedMigrationsAsync();

        Assert.Empty(migracionesPendientes);
        Assert.NotEmpty(migracionesAplicadas);
    }

    [Fact]
    public async Task LasTablasPrincipalesExistenYSonConsultables()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<TransportAppDbContext>();

        // Si la migración no hubiera creado el esquema correctamente, cualquiera
        // de estas consultas lanzaría una excepción de PostgreSQL.
        await contexto.Empresas.CountAsync();
        await contexto.Usuarios.CountAsync();
        await contexto.Servicios.CountAsync();
        await contexto.ServiciosPasajero.CountAsync();
        await contexto.Incidencias.CountAsync();
        await contexto.TokensVerificacion.CountAsync();
    }
}
