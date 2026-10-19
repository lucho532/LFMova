using LFMova.Application.DTOs.Soportes;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Convierte el informe de asistencia de un período en un archivo Excel (.xlsx). Solo da formato al
/// contenido que recibe: no consulta datos ni decide qué pasajeros incluye.
/// </summary>
public interface IGeneradorInformeAsistenciaPeriodo
{
    /// <summary>Genera el archivo Excel del informe.</summary>
    byte[] Generar(InformeAsistenciaPeriodo informe);
}
