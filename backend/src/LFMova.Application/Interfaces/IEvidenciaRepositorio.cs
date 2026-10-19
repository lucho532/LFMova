using LFMova.Domain.Entities;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="Evidencia"/> para la capa de aplicación. No decide reglas de
/// negocio ni de autorización.
/// </summary>
public interface IEvidenciaRepositorio
{
    /// <summary>Obtiene las evidencias de la incidencia indicada.</summary>
    Task<List<Evidencia>> ObtenerPorIncidenciaAsync(int incidenciaId);

    /// <summary>Agrega una nueva evidencia.</summary>
    Task AgregarAsync(Evidencia evidencia);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
