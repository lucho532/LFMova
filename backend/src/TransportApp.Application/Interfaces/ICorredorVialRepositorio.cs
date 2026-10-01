using TransportApp.Domain.Entities;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="CorredorVial"/>. No decide reglas de negocio ni de
/// autorización.
/// </summary>
public interface ICorredorVialRepositorio
{
    /// <summary>Obtiene un corredor vial por su identificador, o <c>null</c> si no existe.</summary>
    Task<CorredorVial?> ObtenerPorIdAsync(int corredorVialId);

    /// <summary>Obtiene todos los corredores viales de una empresa.</summary>
    Task<List<CorredorVial>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Agrega un nuevo corredor vial.</summary>
    Task AgregarAsync(CorredorVial corredorVial);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
