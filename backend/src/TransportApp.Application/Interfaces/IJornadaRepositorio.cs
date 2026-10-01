using TransportApp.Domain.Entities;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre <see cref="Jornada"/>
/// para la capa de aplicación. No decide reglas de negocio ni de autorización.
/// </summary>
public interface IJornadaRepositorio
{
    /// <summary>Obtiene una jornada por su identificador, o <c>null</c> si no existe.</summary>
    Task<Jornada?> ObtenerPorIdAsync(int jornadaId);

    /// <summary>Obtiene las jornadas de la empresa indicada.</summary>
    Task<List<Jornada>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Agrega una nueva jornada.</summary>
    Task AgregarAsync(Jornada jornada);

    /// <summary>Elimina por completo la jornada indicada (borrado físico). Solo es posible si ya no tiene ningún servicio.</summary>
    Task EliminarAsync(Jornada jornada);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
