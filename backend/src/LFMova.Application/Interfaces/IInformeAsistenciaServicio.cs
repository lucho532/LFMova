using LFMova.Application.DTOs.Soportes;
using LFMova.Domain.Enums;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Genera el informe de asistencia de una ruta para enviarlo a la empresa (quiénes asistieron y quiénes
/// no). Solo lee: no modifica estados ni decide autorización de acceso, que se verifica en la capa de Api.
/// </summary>
public interface IInformeAsistenciaServicio
{
    /// <summary>
    /// Devuelve el Excel con los pasajeros de la ruta que cumplen el filtro. Falla si la ruta no existe
    /// en la empresa indicada.
    /// </summary>
    Task<ArchivoSoporte> GenerarAsync(int empresaId, int servicioId, FiltroAsistenciaRuta filtro);
}
