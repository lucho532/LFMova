using LFMova.Application.DTOs.Soportes;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Consulta de solo lectura con los pasajeros de las rutas de una empresa en un rango de fechas, junto con
/// los datos de su ruta y conductor. No decide qué pasajeros cuentan como asistentes ni de autorización.
/// </summary>
public interface IInformeAsistenciaRepositorio
{
    /// <summary>
    /// Pasajeros de las rutas no canceladas de la empresa entre las dos fechas (ambas incluidas). Cada
    /// pasajero trae la ruta, el conductor y la placa que tenía asignados.
    /// </summary>
    Task<List<PasajeroDeRutaDto>> ObtenerPasajerosAsync(int empresaId, DateOnly desde, DateOnly hasta);
}
