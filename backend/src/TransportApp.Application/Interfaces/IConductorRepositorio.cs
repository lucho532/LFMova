using TransportApp.Domain.Entities;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre <see cref="Conductor"/>
/// y <see cref="VinculacionConductorEmpresa"/> para la capa de aplicación. No
/// decide reglas de negocio ni de autorización.
/// </summary>
public interface IConductorRepositorio
{
    /// <summary>
    /// Obtiene un conductor por su identificador, incluyendo su
    /// <see cref="Conductor.Usuario"/> y sus vinculaciones con empresas, o
    /// <c>null</c> si no existe.
    /// </summary>
    Task<Conductor?> ObtenerPorIdAsync(int conductorId);

    /// <summary>
    /// Obtiene el conductor asociado a un usuario, incluyendo sus
    /// vinculaciones con empresas, o <c>null</c> si el usuario no tiene un
    /// perfil de conductor.
    /// </summary>
    Task<Conductor?> ObtenerPorUsuarioIdAsync(int usuarioId);

    /// <summary>Obtiene los conductores vinculados a la empresa indicada.</summary>
    Task<List<Conductor>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Agrega un nuevo conductor.</summary>
    Task AgregarAsync(Conductor conductor);

    /// <summary>Agrega una nueva vinculación entre un conductor y una empresa.</summary>
    Task AgregarVinculacionAsync(VinculacionConductorEmpresa vinculacion);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
