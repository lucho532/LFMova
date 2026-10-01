namespace LFMova.IntegrationTests.Fixtures;

/// <summary>
/// Colección de xUnit que comparte un único contenedor de PostgreSQL (ver
/// <see cref="IntegrationTestFixture"/>) entre todas las pruebas de
/// integración, evitando levantar un contenedor por clase de prueba.
/// </summary>
[CollectionDefinition(Nombre)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    /// <summary>Nombre de la colección compartida de pruebas de integración.</summary>
    public const string Nombre = "PostgreSql";
}
