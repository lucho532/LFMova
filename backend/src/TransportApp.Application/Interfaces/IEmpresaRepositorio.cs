using TransportApp.Domain.Entities;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="Empresa"/>. No decide reglas de negocio ni de autorización.
/// </summary>
public interface IEmpresaRepositorio
{
    /// <summary>Obtiene una empresa por su identificador, o <c>null</c> si no existe.</summary>
    Task<Empresa?> ObtenerPorIdAsync(int empresaId);

    /// <summary>Obtiene una empresa por su CIF, o <c>null</c> si no existe.</summary>
    Task<Empresa?> ObtenerPorCifAsync(string cif);

    /// <summary>Obtiene todas las empresas registradas.</summary>
    Task<List<Empresa>> ObtenerTodasAsync();

    /// <summary>Agrega una nueva empresa.</summary>
    Task AgregarAsync(Empresa empresa);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
