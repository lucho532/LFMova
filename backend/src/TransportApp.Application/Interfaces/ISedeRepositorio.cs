using TransportApp.Domain.Entities;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre <see cref="Sede"/>.
/// No decide reglas de negocio ni de autorización.
/// </summary>
public interface ISedeRepositorio
{
    /// <summary>Obtiene una sede por su identificador, o <c>null</c> si no existe.</summary>
    Task<Sede?> ObtenerPorIdAsync(int sedeId);

    /// <summary>Obtiene todas las sedes de una empresa.</summary>
    Task<List<Sede>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Agrega una nueva sede.</summary>
    Task AgregarAsync(Sede sede);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
