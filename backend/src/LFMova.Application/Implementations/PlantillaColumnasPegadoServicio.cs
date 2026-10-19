using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa los casos de uso sobre la plantilla de orden de columnas para
/// crear rutas pegando filas de un Excel externo. No decide autorización:
/// eso se verifica en la capa de Api.
/// </summary>
public class PlantillaColumnasPegadoServicio : IPlantillaColumnasPegadoServicio
{
    /// <summary>Campos obligatorios: deben aparecer exactamente una vez cada uno.</summary>
    private static readonly string[] CamposObligatorios = { "CEDULA", "NOMBRE", "CELULAR", "DIRECCION", "BARRIO" };

    /// <summary>
    /// "APELLIDOS" es opcional (puede no aparecer, si el Excel trae el nombre completo en una sola
    /// columna de "NOMBRE"), pero si aparece, solo puede aparecer una vez. "IGNORAR" marca una columna
    /// del Excel que no corresponde a ningún dato del pasajero (por ejemplo "Área" o "Cargo"): puede
    /// aparecer cualquier cantidad de veces, incluida ninguna.
    /// </summary>
    private const string CampoApellidos = "APELLIDOS";

    private const string CampoIgnorar = "IGNORAR";

    private readonly IPlantillaColumnasPegadoRepositorio _repositorio;

    /// <summary>Crea el servicio con su repositorio.</summary>
    public PlantillaColumnasPegadoServicio(IPlantillaColumnasPegadoRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    /// <inheritdoc />
    public async Task<PlantillaColumnasPegadoDto?> ObtenerPorEmpresaAsync(int empresaId)
    {
        var plantilla = await _repositorio.ObtenerPorEmpresaAsync(empresaId);
        return plantilla is null ? null : ADto(plantilla);
    }

    /// <inheritdoc />
    public async Task<PlantillaColumnasPegadoDto> GuardarAsync(int empresaId, GuardarPlantillaColumnasPegadoDto datos)
    {
        var columnas = datos.ColumnasEnOrden.Select(c => c.Trim().ToUpperInvariant()).ToList();

        var camposReconocidos = CamposObligatorios.Append(CampoApellidos).Append(CampoIgnorar);
        var noReconocidos = columnas.Except(camposReconocidos).ToList();
        if (noReconocidos.Count > 0)
        {
            throw new InvalidOperationException(
                $"Estas columnas no son un dato reconocido: {string.Join(", ", noReconocidos)}. Los datos válidos son: "
                + $"{string.Join(", ", CamposObligatorios)}, {CampoApellidos} (opcional) o {CampoIgnorar} (para una columna que no se usa).");
        }

        var faltantes = CamposObligatorios.Where(campo => columnas.Count(c => c == campo) != 1).ToList();
        if (faltantes.Count > 0)
        {
            throw new InvalidOperationException($"Estos datos son obligatorios y deben aparecer una sola vez cada uno: {string.Join(", ", faltantes)}.");
        }

        if (columnas.Count(c => c == CampoApellidos) > 1)
        {
            throw new InvalidOperationException($"{CampoApellidos} no puede repetirse.");
        }

        var plantilla = await _repositorio.ObtenerPorEmpresaAsync(empresaId);
        if (plantilla is null)
        {
            plantilla = new PlantillaColumnasPegado { EmpresaId = empresaId, ColumnasEnOrden = columnas };
            await _repositorio.AgregarAsync(plantilla);
        }
        else
        {
            plantilla.ColumnasEnOrden = columnas;
        }

        await _repositorio.GuardarCambiosAsync();
        return ADto(plantilla);
    }

    private static PlantillaColumnasPegadoDto ADto(PlantillaColumnasPegado plantilla) => new()
    {
        PlantillaColumnasPegadoId = plantilla.PlantillaColumnasPegadoId,
        ColumnasEnOrden = new List<string>(plantilla.ColumnasEnOrden)
    };
}
