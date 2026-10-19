using LFMova.Domain.Entities;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Persistencia de los números internos que cada empresa asigna a sus unidades operativas
/// (<see cref="NumeroInternoUnidad"/>). No decide reglas de negocio ni de autorización.
/// </summary>
public interface INumeroInternoUnidadRepositorio
{
    /// <summary>Números internos que la empresa ha asignado a sus unidades.</summary>
    Task<List<NumeroInternoUnidad>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Número que la empresa le asignó a la unidad, o <c>null</c> si no tiene.</summary>
    Task<NumeroInternoUnidad?> ObtenerAsync(int empresaId, int unidadOperativaId);

    /// <summary>Indica si la empresa ya usa ese número (sin distinguir mayúsculas) en una unidad distinta de <paramref name="exceptoUnidadOperativaId"/>.</summary>
    Task<bool> ExisteEnOtraUnidadAsync(int empresaId, string numero, int exceptoUnidadOperativaId);

    /// <summary>Agrega un número interno.</summary>
    Task AgregarAsync(NumeroInternoUnidad numeroInterno);

    /// <summary>Elimina un número interno.</summary>
    void Eliminar(NumeroInternoUnidad numeroInterno);

    /// <summary>Persiste los cambios pendientes.</summary>
    Task GuardarCambiosAsync();
}
