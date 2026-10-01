using LFMova.Domain.Entities;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="PlantillaColumnasPegado"/> para la capa de aplicación. No
/// decide reglas de negocio ni de autorización.
/// </summary>
public interface IPlantillaColumnasPegadoRepositorio
{
    /// <summary>Obtiene la plantilla de la empresa indicada, o <c>null</c> si todavía no definió ninguna.</summary>
    Task<PlantillaColumnasPegado?> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Agrega una nueva plantilla.</summary>
    Task AgregarAsync(PlantillaColumnasPegado plantilla);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
