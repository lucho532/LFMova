using LFMova.Application.DTOs.Importaciones;
using LFMova.Domain.Enums;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Importa un Excel cuyas rutas ya vienen armadas (por ejemplo, por una herramienta externa) respetándolas
/// tal como están: no las reparte, no las reordena ni mueve pasajeros. No decide autorización: eso se
/// verifica en la capa de Api.
/// </summary>
public interface IImportacionRutasArmadasServicio
{
    /// <summary>Interpreta el archivo y devuelve las rutas que contiene y qué pasaría al importarlo. No guarda nada.</summary>
    Task<VistaPreviaRutasArmadasDto> ValidarAsync(int empresaId, Stream archivo, ModoSeparacionRutas modo, string? textoInicioRuta);

    /// <summary>
    /// Crea una ruta por cada ruta del archivo, sin publicarlas. <paramref name="fechaOperativa"/> es la fecha de
    /// las rutas que no la traen y <paramref name="sedeGeneralId"/> la sede de las que no la traen.
    /// </summary>
    Task<ResultadoImportacionDto> ImportarAsync(
        int empresaId, int usuarioCoordinadorId, string nombreArchivo, Stream archivo, ModoSeparacionRutas modo, string? textoInicioRuta, DateOnly fechaOperativa, int? sedeGeneralId);
}
