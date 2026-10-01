using LFMova.Domain.Entities;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre <see cref="Usuario"/>
/// para la capa de aplicación. No decide reglas de negocio ni de autorización.
/// </summary>
public interface IUsuarioRepositorio
{
    /// <summary>
    /// Obtiene un usuario por su identificador interno, junto con sus roles
    /// asignados, o <c>null</c> si no existe.
    /// </summary>
    Task<Usuario?> ObtenerPorIdAsync(int usuarioId);

    /// <summary>
    /// Obtiene un usuario por su cédula junto con sus roles asignados
    /// (<see cref="Usuario.UsuarioRoles"/>), o <c>null</c> si no existe.
    /// </summary>
    Task<Usuario?> ObtenerPorCedulaConRolesAsync(string cedula);

    /// <summary>
    /// Obtiene un usuario por su cédula o por su correo electrónico (para
    /// permitir el inicio de sesión con cualquiera de los dos), junto con sus
    /// roles asignados, o <c>null</c> si no existe.
    /// </summary>
    Task<Usuario?> ObtenerPorIdentificadorConRolesAsync(string identificador);

    /// <summary>Obtiene un usuario por su correo electrónico, o <c>null</c> si no existe.</summary>
    Task<Usuario?> ObtenerPorEmailAsync(string email);

    /// <summary>Agrega un nuevo usuario.</summary>
    Task AgregarAsync(Usuario usuario);

    /// <summary>
    /// Descarta un usuario que se había agregado pero cuyo <see cref="GuardarCambiosAsync"/> falló (por
    /// ejemplo, por una carrera de importaciones concurrentes que chocó con la cédula única): sin esto, el
    /// contexto lo seguiría reintentando en cada guardado posterior y fallaría siempre con el mismo error.
    /// </summary>
    void Descartar(Usuario usuario);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
