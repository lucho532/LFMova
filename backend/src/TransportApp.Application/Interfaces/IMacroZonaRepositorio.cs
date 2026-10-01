using TransportApp.Domain.Entities;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="MacroZona"/>. No decide reglas de negocio ni de autorización.
/// </summary>
public interface IMacroZonaRepositorio
{
    /// <summary>Obtiene una macrozona por su identificador, o <c>null</c> si no existe.</summary>
    Task<MacroZona?> ObtenerPorIdAsync(int macroZonaId);

    /// <summary>Obtiene todas las macrozonas de una empresa.</summary>
    Task<List<MacroZona>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Agrega una nueva macrozona.</summary>
    Task AgregarAsync(MacroZona macroZona);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
