using LFMova.Application.DTOs.Soportes;
using LFMova.Domain.Enums;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Arma el informe de asistencia de todas las rutas de la empresa en una jornada o rango de días, para verlo
/// en pantalla o descargarlo en un único Excel y enviarlo a la empresa. Solo lee: no modifica estados ni decide autorización de
/// acceso, que se verifica en la capa de Api.
/// </summary>
public interface IInformeAsistenciaPeriodoServicio
{
    /// <summary>
    /// Devuelve los pasajeros que cumplen el filtro en las rutas no canceladas entre las dos fechas (ambas
    /// incluidas), ya ordenados: es la vista previa y lo mismo que lleva el Excel. Falla con
    /// <see cref="ArgumentException"/> si el rango es inválido.
    /// </summary>
    Task<InformeAsistenciaPeriodo> ObtenerAsync(int empresaId, DateOnly desde, DateOnly hasta, FiltroAsistenciaRuta filtro);

    /// <summary>
    /// Devuelve el Excel con los pasajeros que cumplen el filtro en las rutas no canceladas entre las dos
    /// fechas (ambas incluidas). Falla con <see cref="ArgumentException"/> si el rango es inválido.
    /// </summary>
    Task<ArchivoSoporte> GenerarAsync(int empresaId, DateOnly desde, DateOnly hasta, FiltroAsistenciaRuta filtro);
}
