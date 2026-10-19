using LFMova.Application.DTOs.Importaciones;
using LFMova.Domain.Enums;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Interpreta un archivo Excel cuyas rutas ya vienen armadas y lo convierte en una
/// <see cref="HojaRutasArmadas"/>, separando las rutas según el modo indicado. No reparte, no reordena,
/// no valida contra la empresa ni persiste nada.
/// </summary>
public interface IExcelRutasArmadasLector
{
    /// <summary>
    /// Lee el archivo. Los problemas de contenido se devuelven en <see cref="HojaRutasArmadas.Errores"/>;
    /// solo lanza excepción si el archivo no es un Excel legible. <paramref name="textoInicioRuta"/> solo
    /// se usa con <see cref="ModoSeparacionRutas.LINEA_INICIO"/>.
    /// </summary>
    HojaRutasArmadas Leer(Stream archivo, ModoSeparacionRutas modo, string? textoInicioRuta);
}
