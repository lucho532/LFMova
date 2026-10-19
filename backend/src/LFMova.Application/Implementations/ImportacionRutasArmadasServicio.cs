using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Implementations.Importaciones;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa la importación de un Excel con rutas ya armadas. Coordina al analizador (que lee y valida
/// el archivo) y al ejecutor (que crea las rutas tal como vienen) y deja registrada la importación. Cada
/// importación crea rutas nuevas: no reutiliza ni completa rutas existentes, y una persona que ya está en
/// otra ruta del mismo horario se omite con un aviso. No decide autorización ni publica las rutas.
/// </summary>
public class ImportacionRutasArmadasServicio : IImportacionRutasArmadasServicio
{
    private readonly AnalizadorRutasArmadas _analizador;
    private readonly EjecutorRutasArmadas _ejecutor;
    private readonly IImportacionExcelRepositorio _importacionRepositorio;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ImportacionRutasArmadasServicio(AnalizadorRutasArmadas analizador, EjecutorRutasArmadas ejecutor, IImportacionExcelRepositorio importacionRepositorio)
    {
        _analizador = analizador;
        _ejecutor = ejecutor;
        _importacionRepositorio = importacionRepositorio;
    }

    /// <inheritdoc />
    public async Task<VistaPreviaRutasArmadasDto> ValidarAsync(int empresaId, Stream archivo, ModoSeparacionRutas modo, string? textoInicioRuta)
        => (await _analizador.AnalizarAsync(empresaId, archivo, modo, textoInicioRuta)).Vista;

    /// <inheritdoc />
    public async Task<ResultadoImportacionDto> ImportarAsync(
        int empresaId, int usuarioCoordinadorId, string nombreArchivo, Stream archivo, ModoSeparacionRutas modo, string? textoInicioRuta, DateOnly fechaOperativa, int? sedeGeneralId)
    {
        var analisis = await _analizador.AnalizarAsync(empresaId, archivo, modo, textoInicioRuta);
        if (!analisis.Vista.PuedeImportar)
        {
            throw new InvalidOperationException("El archivo tiene errores y no se puede importar: " + string.Join(" ", analisis.Vista.Errores));
        }

        var resultado = new ResultadoImportacionDto { Advertencias = new List<string>(analisis.Vista.Advertencias) };
        try
        {
            await _ejecutor.EjecutarAsync(empresaId, analisis, fechaOperativa, sedeGeneralId, resultado);
        }
        catch
        {
            await RegistrarAsync(empresaId, usuarioCoordinadorId, nombreArchivo, EstadoImportacionExcel.ERROR);
            throw;
        }

        await RegistrarAsync(
            empresaId, usuarioCoordinadorId, nombreArchivo,
            resultado.Advertencias.Count > 0 ? EstadoImportacionExcel.COMPLETADA_CON_ADVERTENCIAS : EstadoImportacionExcel.COMPLETADA);
        return resultado;
    }

    private async Task RegistrarAsync(int empresaId, int usuarioCoordinadorId, string nombreArchivo, EstadoImportacionExcel estado)
    {
        await _importacionRepositorio.AgregarAsync(new ImportacionExcel
        {
            EmpresaId = empresaId,
            CoordinadorId = usuarioCoordinadorId,
            NombreArchivo = nombreArchivo,
            FechaImportacion = DateTime.UtcNow,
            Estado = estado
        });
        await _importacionRepositorio.GuardarCambiosAsync();
    }
}
