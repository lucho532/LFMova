using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="UsuarioRol"/>. No decide reglas de negocio ni de autorización.
/// </summary>
public interface IUsuarioRolRepositorio
{
    /// <summary>Obtiene un <see cref="UsuarioRol"/> por su identificador, o <c>null</c> si no existe.</summary>
    Task<UsuarioRol?> ObtenerPorIdAsync(int usuarioRolId);

    /// <summary>
    /// Obtiene los roles del tipo indicado (activos e inactivos) asignados a
    /// una empresa concreta.
    /// </summary>
    Task<List<UsuarioRol>> ObtenerPorEmpresaYRolAsync(int empresaId, Rol rol);

    /// <summary>Agrega un nuevo <see cref="UsuarioRol"/>.</summary>
    Task AgregarAsync(UsuarioRol usuarioRol);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
