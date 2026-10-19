using System.Globalization;
using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Domain.Entities;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Resuelve a qué sede de la empresa corresponde el nombre que trae la hoja importada, creándola si
/// todavía no existe (las sedes vienen del Excel, no se digitan aparte). No interpreta el archivo ni
/// crea servicios.
/// </summary>
public class ResolutorSedeImportacion
{
    private readonly ISedeRepositorio _sedeRepositorio;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public ResolutorSedeImportacion(ISedeRepositorio sedeRepositorio)
    {
        _sedeRepositorio = sedeRepositorio;
    }

    /// <summary>Devuelve las sedes activas de la empresa, contra las que se comparan los nombres de la hoja.</summary>
    public async Task<List<Sede>> ObtenerActivasAsync(int empresaId) =>
        (await _sedeRepositorio.ObtenerPorEmpresaAsync(empresaId)).Where(s => s.Activa).ToList();

    /// <summary>
    /// Busca la sede cuyo nombre coincide con el de la hoja (sin mayúsculas ni tildes) o, si ninguna
    /// coincide exactamente, la primera que lo contenga. Devuelve <c>null</c> si la hoja no trae nombre
    /// o si no hay ninguna parecida.
    /// </summary>
    public static Sede? Buscar(List<Sede> sedes, string nombreEnHoja)
    {
        var buscado = TextoNormalizador.Normalizar(nombreEnHoja);
        if (buscado.Length == 0)
        {
            return null;
        }

        return sedes.FirstOrDefault(s => TextoNormalizador.Normalizar(s.Nombre) == buscado)
            ?? sedes.FirstOrDefault(s => TextoNormalizador.Normalizar(s.Nombre).Contains(buscado));
    }

    /// <summary>
    /// Devuelve la sede del grupo: la que coincide con el nombre de la hoja;
    /// si el nombre no existe todavía, la crea (las sedes vienen del Excel,
    /// no se digitan aparte). Si la hoja no trae sede (columna ausente o
    /// celda vacía), usa la sede general que eligió el coordinador al
    /// importar; si tampoco la indicó, falla.
    /// </summary>
    public async Task<Sede> ObtenerOCrearAsync(int empresaId, string nombreEnHoja, int? sedeGeneralId, List<Sede> sedes, ResultadoImportacionDto resultado)
    {
        var existente = Buscar(sedes, nombreEnHoja);
        if (existente is not null)
        {
            return existente;
        }

        if (nombreEnHoja.Trim().Length > 0)
        {
            var nombre = CultureInfo.GetCultureInfo("es-CO").TextInfo.ToTitleCase(nombreEnHoja.Trim().ToLowerInvariant());
            var sede = new Sede
            {
                EmpresaId = empresaId,
                Nombre = nombre,
                Direccion = "Por definir",
                Ciudad = "Por definir",
                Barrio = "Por definir",
                Activa = true
            };
            await _sedeRepositorio.AgregarAsync(sede);
            await _sedeRepositorio.GuardarCambiosAsync();
            sedes.Add(sede);
            resultado.Advertencias.Add($"Se creó la sede \"{nombre}\" con dirección por definir.");
            return sede;
        }

        var general = sedeGeneralId is not null ? sedes.FirstOrDefault(s => s.SedeId == sedeGeneralId.Value) : null;
        if (general is not null)
        {
            return general;
        }

        throw new InvalidOperationException("El archivo no trae la sede de algunos pasajeros: elige una sede al importar.");
    }
}
