using System.Globalization;
using ClosedXML.Excel;
using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Utils;
using LFMova.Domain.Enums;
using static LFMova.Infrastructure.Excel.CamposHoja;
using static LFMova.Infrastructure.Excel.CeldasHoja;

namespace LFMova.Infrastructure.Excel;

/// <summary>
/// Lee el formato de tabla plana: una fila por pasajero con columnas de sede, fecha y hora de entrada
/// o salida, y las agrupa por tipo, sede, fecha y hora. No valida los datos contra la empresa.
/// </summary>
internal static class LectorTablaPlana
{
    internal static void LeerTablaPlana(IXLWorksheet hoja, int filaEncabezado, int ultimaFila, Dictionary<string, int> columnas, HojaProgramacion resultado)
    {
        var grupos = new Dictionary<(TipoServicio, string, DateOnly?, TimeOnly), GrupoHoja>();
        DateOnly? primera = null;
        DateOnly? ultima = null;

        for (var fila = filaEncabezado + 1; fila <= ultimaFila; fila++)
        {
            var cedula = LimpiarNumero(hoja.Cell(fila, columnas[Cedula]));
            if (cedula.Length == 0)
            {
                continue;
            }

            // Sin sede en la hoja: la fila igual se lee, la sede la elige el coordinador al importar.
            var sede = columnas.TryGetValue(Sede, out var columnaSede) ? Texto(hoja.Cell(fila, columnaSede)).Trim() : string.Empty;

            DateOnly? fecha = null;
            if (columnas.TryGetValue(Fecha, out var columnaFecha) && !hoja.Cell(fila, columnaFecha).IsEmpty())
            {
                if (!TryLeerFecha(hoja.Cell(fila, columnaFecha), out var leida))
                {
                    resultado.Errores.Add($"Fila {fila}: la fecha \"{Texto(hoja.Cell(fila, columnaFecha)).Trim()}\" no es válida.");
                    continue;
                }

                fecha = leida;
                primera = primera is null || leida < primera ? leida : primera;
                ultima = ultima is null || leida > ultima ? leida : ultima;
            }

            var horas = new List<(TipoServicio Tipo, TimeOnly Hora)>();
            var invalida = false;
            foreach (var (campo, tipo) in new[] { (Entrada, TipoServicio.ENTRADA), (Salida, TipoServicio.SALIDA) })
            {
                if (!columnas.TryGetValue(campo, out var columna) || hoja.Cell(fila, columna).IsEmpty())
                {
                    continue;
                }

                if (TryLeerHora(hoja.Cell(fila, columna), out var hora))
                {
                    horas.Add((tipo, hora));
                }
                else
                {
                    resultado.Errores.Add($"Fila {fila}: la hora de {campo.ToLowerInvariant()} \"{Texto(hoja.Cell(fila, columna)).Trim()}\" no es válida.");
                    invalida = true;
                }
            }

            if (invalida)
            {
                continue;
            }

            if (horas.Count == 0)
            {
                resultado.Errores.Add($"Fila {fila}: no tiene hora de entrada ni de salida.");
                continue;
            }

            var pasajero = CrearFila(hoja, fila, cedula, columnas);
            foreach (var (tipo, hora) in horas)
            {
                var clave = (tipo, TextoNormalizador.Normalizar(sede), fecha, hora);
                if (!grupos.TryGetValue(clave, out var grupo))
                {
                    grupo = new GrupoHoja
                    {
                        Titulo = $"{(tipo == TipoServicio.ENTRADA ? "ENTRADA" : "SALIDA")} {sede.ToUpperInvariant()}",
                        Tipo = tipo,
                        NombreSede = sede,
                        Hora = hora,
                        Fecha = fecha
                    };
                    grupos[clave] = grupo;
                    resultado.Grupos.Add(grupo);
                }

                grupo.Filas.Add(pasajero);
            }
        }

        resultado.FechasPorFila = primera is not null;
        if (primera is not null)
        {
            resultado.FechaTexto = primera == ultima
                ? primera.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                : $"{primera.Value:dd/MM/yyyy} a {ultima!.Value:dd/MM/yyyy}";
        }
    }
}
