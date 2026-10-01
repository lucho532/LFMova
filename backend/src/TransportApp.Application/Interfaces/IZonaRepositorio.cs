using TransportApp.Domain.Entities;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre <see cref="Zona"/>.
/// No decide reglas de negocio ni de autorización.
/// </summary>
public interface IZonaRepositorio
{
    /// <summary>Obtiene una zona por su identificador, o <c>null</c> si no existe.</summary>
    Task<Zona?> ObtenerPorIdAsync(int zonaId);

    /// <summary>Obtiene todas las zonas de una empresa.</summary>
    Task<List<Zona>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Agrega una nueva zona.</summary>
    Task AgregarAsync(Zona zona);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
