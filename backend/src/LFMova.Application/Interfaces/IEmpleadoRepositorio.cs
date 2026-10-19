using LFMova.Domain.Entities;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre <see cref="Empleado"/>
/// para la capa de aplicación. No decide reglas de negocio ni de autorización.
/// </summary>
public interface IEmpleadoRepositorio
{
    /// <summary>
    /// Obtiene un empleado por su identificador, incluyendo su
    /// <see cref="Empleado.Usuario"/>, o <c>null</c> si no existe.
    /// </summary>
    Task<Empleado?> ObtenerPorIdAsync(int empleadoId);

    /// <summary>
    /// Obtiene el perfil de empleado asociado a un usuario, o <c>null</c> si
    /// el usuario no tiene un perfil de empleado.
    /// </summary>
    Task<Empleado?> ObtenerPorUsuarioIdAsync(int usuarioId);

    /// <summary>Obtiene los empleados de la empresa indicada.</summary>
    Task<List<Empleado>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Agrega un nuevo empleado.</summary>
    Task AgregarAsync(Empleado empleado);

    /// <summary>
    /// Descarta un empleado que se había agregado pero cuyo <see cref="GuardarCambiosAsync"/> falló (por
    /// ejemplo, por una carrera de importaciones concurrentes que chocó con el UsuarioId único): sin esto,
    /// el contexto lo seguiría reintentando en cada guardado posterior y fallaría siempre con el mismo error.
    /// </summary>
    void Descartar(Empleado empleado);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
