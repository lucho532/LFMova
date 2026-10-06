using LFMova.Domain.Entities;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia sobre <see cref="RegistroLlamada"/>
/// para la capa de aplicación. No decide reglas de negocio ni de autorización.
/// </summary>
public interface IRegistroLlamadaRepositorio
{
    /// <summary>Obtiene un registro por su identificador, o <c>null</c> si no existe.</summary>
    Task<RegistroLlamada?> ObtenerPorIdAsync(int registroLlamadaId);

    /// <summary>Obtiene las llamadas hechas al pasajero indicado, de la más antigua a la más reciente.</summary>
    Task<List<RegistroLlamada>> ObtenerPorServicioPasajeroAsync(int servicioPasajeroId);

    /// <summary>Agrega un nuevo registro.</summary>
    Task AgregarAsync(RegistroLlamada registro);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
