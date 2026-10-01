using LFMova.Application.DTOs.Importaciones;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Interpreta un archivo Excel de programación diaria de un conductor y lo
/// convierte en una <see cref="HojaProgramacion"/>. No valida los datos contra
/// la empresa ni persiste nada: eso corresponde al servicio de importación.
/// </summary>
public interface IExcelProgramacionLector
{
    /// <summary>
    /// Lee la primera hoja del archivo. Los problemas de formato se devuelven
    /// en <see cref="HojaProgramacion.Errores"/>; solo lanza excepción si el
    /// archivo no es un Excel legible.
    /// </summary>
    HojaProgramacion Leer(Stream archivo);
}
