using LFMova.Application.DTOs.Soportes;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Convierte un informe de asistencia de una ruta en un archivo Excel (.xlsx). Solo da formato al
/// contenido que recibe: no consulta datos ni decide qué pasajeros incluye.
/// </summary>
public interface IGeneradorInformeAsistencia
{
    /// <summary>Genera el archivo Excel del informe.</summary>
    byte[] Generar(InformeAsistenciaRuta informe);
}
