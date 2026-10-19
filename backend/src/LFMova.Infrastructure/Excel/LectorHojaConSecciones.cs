using ClosedXML.Excel;
using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Utils;
using LFMova.Domain.Enums;
using static LFMova.Infrastructure.Excel.CamposHoja;
using static LFMova.Infrastructure.Excel.CeldasHoja;

namespace LFMova.Infrastructure.Excel;

/// <summary>
/// Lee el formato con secciones: filas "ENTRADA &lt;SEDE&gt;" / "SALIDA &lt;SEDE&gt;" que abren cada grupo de
/// pasajeros. No valida los datos contra la empresa.
/// </summary>
internal static class LectorHojaConSecciones
{
    internal static void LeerConSecciones(IXLWorksheet hoja, int filaEncabezado, int ultimaFila, Dictionary<string, int> columnas, HojaProgramacion resultado)
    {
        if (!columnas.ContainsKey(Entrada) && !columnas.ContainsKey(Salida))
        {
            resultado.Errores.Add("Falta la columna de hora de entrada o de salida.");
            return;
        }

        string? titulo = null;
        TipoServicio tipo = default;
        var nombreSede = string.Empty;
        // Se agrupa por (tipo, sede, hora) en vez de por la sección inmediatamente anterior: así, si la hoja
        // trae varios bloques de la misma "ENTRADA <sede>" con horas intercaladas (por ejemplo, filas de otra
        // hora en medio), las filas que comparten tipo+sede+hora terminan en el mismo GrupoHoja y se reparten
        // juntas, en vez de abrir una ruta nueva por cada bloque y agotar unidades disponibles innecesariamente.
        var grupos = new Dictionary<(TipoServicio Tipo, string Sede, TimeOnly Hora), GrupoHoja>();

        for (var fila = filaEncabezado + 1; fila <= ultimaFila; fila++)
        {
            var textoA = Texto(hoja.Cell(fila, columnas[Cedula]));

            if (EsFilaDeSeccion(hoja, fila, columnas, textoA))
            {
                titulo = textoA.Trim();
                var partes = titulo.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                tipo = partes[0].Equals("ENTRADA", StringComparison.OrdinalIgnoreCase) ? TipoServicio.ENTRADA : TipoServicio.SALIDA;
                nombreSede = partes.Length > 1 ? partes[1].Trim() : string.Empty;
                continue;
            }

            var cedula = LimpiarNumero(hoja.Cell(fila, columnas[Cedula]));
            if (cedula.Length == 0)
            {
                continue;
            }

            if (titulo is null)
            {
                resultado.Errores.Add($"Fila {fila}: hay un pasajero antes de cualquier sección \"ENTRADA <sede>\" o \"SALIDA <sede>\".");
                continue;
            }

            var campoHora = tipo == TipoServicio.ENTRADA ? Entrada : Salida;
            if (!columnas.TryGetValue(campoHora, out var columnaHora) || !TryLeerHora(hoja.Cell(fila, columnaHora), out var hora))
            {
                resultado.Errores.Add($"Fila {fila}: la hora de {campoHora.ToLowerInvariant()} no es válida.");
                continue;
            }

            var clave = (tipo, TextoNormalizador.Normalizar(nombreSede), hora);
            if (!grupos.TryGetValue(clave, out var grupoActual))
            {
                grupoActual = new GrupoHoja { Titulo = titulo, Tipo = tipo, NombreSede = nombreSede, Hora = hora };
                grupos[clave] = grupoActual;
                resultado.Grupos.Add(grupoActual);
            }

            grupoActual.Filas.Add(CrearFila(hoja, fila, cedula, columnas));
        }
    }

    internal static bool EsFilaDeSeccion(IXLWorksheet hoja, int fila, Dictionary<string, int> columnas, string textoA)
    {
        var texto = textoA.Trim();
        if (!(texto.StartsWith("ENTRADA ", StringComparison.OrdinalIgnoreCase) || texto.StartsWith("SALIDA ", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return columnas.Where(c => c.Key != Cedula).All(c => Texto(hoja.Cell(fila, c.Value)).Trim().Length == 0);
    }

    /// <summary>Busca en toda la hoja si hay al menos una fila "ENTRADA &lt;SEDE&gt;"/"SALIDA &lt;SEDE&gt;".</summary>
    internal static bool TieneMarcasDeSeccion(IXLWorksheet hoja, int filaEncabezado, int ultimaFila, Dictionary<string, int> columnas)
    {
        for (var fila = filaEncabezado + 1; fila <= ultimaFila; fila++)
        {
            if (EsFilaDeSeccion(hoja, fila, columnas, Texto(hoja.Cell(fila, columnas[Cedula]))))
            {
                return true;
            }
        }

        return false;
    }
}
