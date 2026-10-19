using LFMova.Application.DTOs.Estadisticas;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Consultas de solo lectura que agregan la actividad de una empresa
/// (rutas y pasajeros). No decide reglas de negocio ni de autorización.
/// </summary>
public interface IEstadisticasRepositorio
{
    /// <summary>Resumen histórico de la empresa.</summary>
    Task<ResumenEmpresaDto> ObtenerResumenAsync(int empresaId);

    /// <summary>Actividad de cada conductor vinculado a la empresa en el rango (extremos opcionales).</summary>
    Task<List<EstadisticaConductorDto>> ObtenerPorConductorAsync(int empresaId, DateOnly? desde, DateOnly? hasta);

    /// <summary>Rutas de un conductor en la empresa dentro del rango (extremos opcionales), de la más reciente a la más antigua.</summary>
    Task<List<RutaConductorDto>> ObtenerRutasDeConductorAsync(int empresaId, int conductorId, DateOnly? desde, DateOnly? hasta);

    /// <summary>
    /// Rutas de la empresa. <paramref name="filtro"/>: <c>programadas</c> (todas menos canceladas), <c>realizadas</c>,
    /// <c>por-realizar</c>, <c>canceladas</c> o vacío para todas. Más recientes primero.
    /// </summary>
    Task<List<RutaEmpresaDto>> ObtenerRutasAsync(int empresaId, string? filtro, DateOnly? desde, DateOnly? hasta);

    /// <summary>Pasajeros de las rutas de la empresa (máximo 2000). Con <paramref name="soloTransportados"/> solo los ya dejados en su destino.</summary>
    Task<List<PasajeroEmpresaDto>> ObtenerPasajerosAsync(int empresaId, bool soloTransportados, DateOnly? desde, DateOnly? hasta);
}
