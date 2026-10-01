using TransportApp.Domain.Entities;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="BarreraGeografica"/>. No decide reglas de negocio ni de
/// autorización.
/// </summary>
public interface IBarreraGeograficaRepositorio
{
    /// <summary>Obtiene una barrera por su identificador, o <c>null</c> si no existe.</summary>
    Task<BarreraGeografica?> ObtenerPorIdAsync(int barreraGeograficaId);

    /// <summary>Obtiene todas las barreras geográficas de una empresa.</summary>
    Task<List<BarreraGeografica>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Agrega una nueva barrera geográfica.</summary>
    Task AgregarAsync(BarreraGeografica barrera);

    /// <summary>Elimina una barrera geográfica existente.</summary>
    void Eliminar(BarreraGeografica barrera);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
