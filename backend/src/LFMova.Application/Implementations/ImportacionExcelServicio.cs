using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Implementations.Importaciones;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa la importación de la programación diaria de un conductor desde
/// un Excel. Coordina a los colaboradores de <c>Implementations/Importaciones</c>
/// (análisis del archivo, ejecución y creación de rutas a mano), que reutilizan
/// los servicios existentes (programaciones, jornadas, servicios y pasajeros)
/// para que se apliquen las mismas validaciones que en el alta manual. Es
/// repetible: si se importa dos veces el mismo archivo, lo ya importado se
/// reutiliza y no se duplica. No es una transacción única: si falla a mitad,
/// lo ya guardado se conserva y se puede volver a importar.
/// No decide autorización: eso se verifica en la capa de Api.
/// </summary>
public class ImportacionExcelServicio : IImportacionExcelServicio
{
    private readonly AnalizadorImportacion _analizador;
    private readonly EjecutorImportacion _ejecutor;
    private readonly CreadorRutaVacia _creadorRutaVacia;
    private readonly CreadorRutaPegada _creadorRutaPegada;
    private readonly IImportacionExcelRepositorio _importacionRepositorio;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ImportacionExcelServicio(
        AnalizadorImportacion analizador,
        EjecutorImportacion ejecutor,
        CreadorRutaVacia creadorRutaVacia,
        CreadorRutaPegada creadorRutaPegada,
        IImportacionExcelRepositorio importacionRepositorio)
    {
        _analizador = analizador;
        _ejecutor = ejecutor;
        _creadorRutaVacia = creadorRutaVacia;
        _creadorRutaPegada = creadorRutaPegada;
        _importacionRepositorio = importacionRepositorio;
    }

    /// <inheritdoc />
    public async Task<VistaPreviaImportacionDto> ValidarAsync(int empresaId, Stream archivo)
        => (await _analizador.AnalizarAsync(empresaId, archivo)).Vista;

    /// <inheritdoc />
    public async Task<ResultadoImportacionDto> ImportarAsync(
        int empresaId, int usuarioCoordinadorId, string nombreArchivo, Stream archivo, DateOnly fechaOperativa, int? unidadOperativaId, bool repartirEntreUnidades, int? sedeGeneralId)
    {
        var analisis = await _analizador.AnalizarAsync(empresaId, archivo);
        if (!analisis.Vista.PuedeImportar)
        {
            throw new InvalidOperationException("El archivo tiene errores y no se puede importar: " + string.Join(" ", analisis.Vista.Errores));
        }

        var resultado = new ResultadoImportacionDto
        {
            Advertencias = new List<string>(analisis.Vista.Advertencias),
            BarriosSinZona = new List<string>(analisis.Vista.BarriosSinZona)
        };

        try
        {
            await _ejecutor.EjecutarAsync(empresaId, analisis, fechaOperativa, unidadOperativaId, repartirEntreUnidades, sedeGeneralId, resultado);
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

    /// <inheritdoc />
    public Task<ResultadoImportacionDto> CrearRutaVaciaAsync(int empresaId, CrearRutaVaciaDto datos)
        => _creadorRutaVacia.CrearAsync(empresaId, datos);

    /// <inheritdoc />
    public Task<ResultadoImportacionDto> CrearRutaPegadaAsync(int empresaId, CrearRutaPegadaDto datos)
        => _creadorRutaPegada.CrearAsync(empresaId, datos);

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
