using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using TransportApp.Application.DTOs.Importaciones;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Enums;

namespace TransportApp.Infrastructure.Excel;

/// <summary>
/// Implementa <see cref="IExcelProgramacionLector"/> con ClosedXML y tolera
/// variaciones en los encabezados (sin tildes, mayúsculas, sinónimos como
/// "Numero de celular" o "Entrada"/"ENTRA", y errores de tipeo leves). Entiende
/// dos formatos, que detecta por sus columnas:
/// <list type="bullet">
/// <item><description><b>Tabla plana</b>: una fila por pasajero con columnas de Sede, Fecha y hora de
/// Entrada/Salida. Las filas se agrupan por tipo, sede, fecha y hora.</description></item>
/// <item><description><b>Hoja con secciones</b>: filas "ENTRADA &lt;SEDE&gt;" / "SALIDA &lt;SEDE&gt;" que
/// abren cada grupo, con "TRANSPORTADOR:" y "FECHA:" arriba.</description></item>
/// </list>
/// Las horas aceptan "4AM", "4:30 pm", "16:00", "1600" y horas nativas de Excel.
/// </summary>
public class ExcelProgramacionLectorClosedXml : IExcelProgramacionLector
{
    private const string Cedula = "CEDULA";
    private const string Nombre = "NOMBRE";
    private const string Apellidos = "APELLIDOS";
    private const string Direccion = "DIRECCION";
    private const string Barrio = "BARRIO";
    private const string Celular = "CELULAR";
    private const string Entrada = "ENTRADA";
    private const string Salida = "SALIDA";
    private const string Fecha = "FECHA";
    private const string Sede = "SEDE";

    private static readonly string[] Obligatorios = { Cedula, Nombre, Direccion, Barrio };

    private static readonly string[] FormatosFecha =
    {
        "yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yy", "d/M/yy", "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy HH:mm:ss"
    };

    private static readonly Regex HoraConMeridiano = new(@"^(\d{1,2})(?:\s*[:.hH]\s*(\d{1,2}))?\s*([AaPp])\.?\s*[Mm]?\.?$", RegexOptions.Compiled);
    private static readonly Regex HoraCuatroDigitos = new(@"^(\d{2})(\d{2})$", RegexOptions.Compiled);

    /// <inheritdoc />
    public HojaProgramacion Leer(Stream archivo)
    {
        using var libro = new XLWorkbook(archivo);
        var resultado = new HojaProgramacion();
        var diagnostico = string.Empty;

        foreach (var hoja in libro.Worksheets)
        {
            var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 0;
            var ultimaColumna = Math.Max(hoja.LastColumnUsed()?.ColumnNumber() ?? 0, 8);
            var columnas = BuscarEncabezados(hoja, ultimaFila, ultimaColumna, out var filaEncabezado, out var faltantes);

            if (columnas is null)
            {
                diagnostico = diagnostico.Length == 0 ? faltantes : diagnostico;
                continue;
            }

            LeerCabecera(hoja, filaEncabezado, ultimaColumna, resultado);
            // La sede ya no es obligatoria: si la hoja no trae columna de sede pero sí trae
            // secciones "ENTRADA/SALIDA <SEDE>", se leen esas; si no trae ninguna de las dos,
            // se lee como tabla plana sin sede (la elige el coordinador al importar).
            if (!columnas.ContainsKey(Sede) && TieneMarcasDeSeccion(hoja, filaEncabezado, ultimaFila, columnas))
            {
                LeerConSecciones(hoja, filaEncabezado, ultimaFila, columnas, resultado);
            }
            else
            {
                LeerTablaPlana(hoja, filaEncabezado, ultimaFila, columnas, resultado);
            }

            if (resultado.Grupos.Count == 0 && resultado.Errores.Count == 0)
            {
                resultado.Errores.Add("El archivo no contiene ningún pasajero.");
            }

            return resultado;
        }

        resultado.Errores.Add(
            "No se encontró la fila de encabezados. Se necesitan las columnas: cédula, nombre, dirección, barrio y hora de entrada o de salida "
            + $"(la sede y la fecha son opcionales: si faltan, se eligen al importar). {diagnostico}".TrimEnd());
        return resultado;
    }

    // ---------- Formato tabla plana ----------

    private static void LeerTablaPlana(IXLWorksheet hoja, int filaEncabezado, int ultimaFila, Dictionary<string, int> columnas, HojaProgramacion resultado)
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
                var clave = (tipo, Normalizar(sede), fecha, hora);
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

    // ---------- Formato con secciones ----------

    private static void LeerConSecciones(IXLWorksheet hoja, int filaEncabezado, int ultimaFila, Dictionary<string, int> columnas, HojaProgramacion resultado)
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

            var clave = (tipo, Normalizar(nombreSede), hora);
            if (!grupos.TryGetValue(clave, out var grupoActual))
            {
                grupoActual = new GrupoHoja { Titulo = titulo, Tipo = tipo, NombreSede = nombreSede, Hora = hora };
                grupos[clave] = grupoActual;
                resultado.Grupos.Add(grupoActual);
            }

            grupoActual.Filas.Add(CrearFila(hoja, fila, cedula, columnas));
        }
    }

    private static bool EsFilaDeSeccion(IXLWorksheet hoja, int fila, Dictionary<string, int> columnas, string textoA)
    {
        var texto = textoA.Trim();
        if (!(texto.StartsWith("ENTRADA ", StringComparison.OrdinalIgnoreCase) || texto.StartsWith("SALIDA ", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return columnas.Where(c => c.Key != Cedula).All(c => Texto(hoja.Cell(fila, c.Value)).Trim().Length == 0);
    }

    /// <summary>Busca en toda la hoja si hay al menos una fila "ENTRADA &lt;SEDE&gt;"/"SALIDA &lt;SEDE&gt;".</summary>
    private static bool TieneMarcasDeSeccion(IXLWorksheet hoja, int filaEncabezado, int ultimaFila, Dictionary<string, int> columnas)
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

    // ---------- Encabezados ----------

    private static void LeerCabecera(IXLWorksheet hoja, int filaEncabezado, int ultimaColumna, HojaProgramacion resultado)
    {
        for (var fila = 1; fila < filaEncabezado; fila++)
        {
            for (var columna = 1; columna <= ultimaColumna; columna++)
            {
                var texto = Texto(hoja.Cell(fila, columna)).Trim().TrimEnd(':').ToUpperInvariant();
                if (texto == "TRANSPORTADOR" && resultado.Transportador.Length == 0)
                {
                    resultado.Transportador = SiguienteTexto(hoja, fila, columna, ultimaColumna);
                }
                else if (texto == "FECHA" && resultado.FechaTexto.Length == 0)
                {
                    resultado.FechaTexto = SiguienteTexto(hoja, fila, columna, ultimaColumna);
                }
            }
        }
    }

    private static string SiguienteTexto(IXLWorksheet hoja, int fila, int desdeColumna, int ultimaColumna)
    {
        for (var columna = desdeColumna + 1; columna <= ultimaColumna; columna++)
        {
            var texto = Texto(hoja.Cell(fila, columna)).Trim();
            if (texto.Length > 0)
            {
                return texto;
            }
        }

        return string.Empty;
    }

    /// <summary>Busca, en las primeras filas, la que mejor reconoce como fila de encabezados.</summary>
    private static Dictionary<string, int>? BuscarEncabezados(IXLWorksheet hoja, int ultimaFila, int ultimaColumna, out int filaEncabezado, out string diagnostico)
    {
        Dictionary<string, int>? mejor = null;
        var mejorFila = 0;

        for (var fila = 1; fila <= Math.Min(ultimaFila, 15); fila++)
        {
            var columnas = new Dictionary<string, int>();
            for (var columna = 1; columna <= ultimaColumna; columna++)
            {
                var campo = ReconocerCampo(Texto(hoja.Cell(fila, columna)));
                if (campo is not null && !columnas.ContainsKey(campo))
                {
                    columnas[campo] = columna;
                }
            }

            if (mejor is null || columnas.Count > mejor.Count)
            {
                mejor = columnas;
                mejorFila = fila;
            }

            if (Obligatorios.All(columnas.ContainsKey) && (columnas.ContainsKey(Entrada) || columnas.ContainsKey(Salida)))
            {
                filaEncabezado = fila;
                diagnostico = string.Empty;
                return columnas;
            }
        }

        filaEncabezado = 0;
        var leidos = mejor is null ? "ninguna" : string.Join(", ", mejor.Keys.Select(k => k.ToLowerInvariant()));
        var faltan = Obligatorios.Where(o => mejor is null || !mejor.ContainsKey(o)).Select(o => o.ToLowerInvariant()).ToList();
        if (mejor is null || (!mejor.ContainsKey(Entrada) && !mejor.ContainsKey(Salida)))
        {
            faltan.Add("hora de entrada o de salida");
        }

        diagnostico = $"Columnas reconocidas en la fila {mejorFila}: {leidos}. Faltan: {string.Join(", ", faltan)}.";
        return null;
    }

    /// <summary>
    /// Traduce un encabezado a su campo canónico, o <c>null</c> si no se
    /// reconoce. Ignora tildes, mayúsculas, espacios y signos; acepta
    /// sinónimos y tolera un error de tipeo en palabras largas.
    /// </summary>
    internal static string? ReconocerCampo(string encabezado)
    {
        var clave = new string(Normalizar(encabezado).Where(char.IsLetter).ToArray());
        if (clave.Length == 0)
        {
            return null;
        }

        // Una sola columna "Nombre y apellidos" cuenta como nombre completo.
        if (clave.Contains("NOMBRE") && clave.Contains("APELLIDO"))
        {
            return Nombre;
        }

        // El orden importa: "APELLIDO" debe ganar a "NOMBRE" ("nombre y apellido").
        var reglas = new (string Campo, string[] Palabras)[]
        {
            (Apellidos, new[] { "APELLIDO" }),
            (Cedula, new[] { "CEDULA", "DOCUMENTO", "IDENTIFICACION", "CC", "ID" }),
            (Celular, new[] { "CELULAR", "TELEFONO", "MOVIL", "WHATSAPP" }),
            (Direccion, new[] { "DIRECCION", "RECOGIDA", "DOMICILIO" }),
            (Barrio, new[] { "BARRIO", "LOCALIDAD", "ZONA" }),
            (Sede, new[] { "SEDE", "PUNTO", "DESTINO" }),
            (Fecha, new[] { "FECHA", "DIA" }),
            (Entrada, new[] { "ENTRADA", "ENTRA", "INGRESO" }),
            (Salida, new[] { "SALIDA", "SALE", "EGRESO" }),
            (Nombre, new[] { "NOMBRE" }),
        };

        foreach (var (campo, palabras) in reglas)
        {
            foreach (var palabra in palabras)
            {
                if (clave == palabra || (palabra.Length >= 4 && clave.Contains(palabra)) || (palabra.Length >= 6 && Distancia(clave, palabra) <= 1))
                {
                    return campo;
                }
            }
        }

        return null;
    }

    private static int Distancia(string a, string b)
    {
        if (Math.Abs(a.Length - b.Length) > 1)
        {
            return 2;
        }

        var previa = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var actual = new int[b.Length + 1];
            actual[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                actual[j] = Math.Min(Math.Min(actual[j - 1] + 1, previa[j] + 1), previa[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            }

            previa = actual;
        }

        return previa[b.Length];
    }

    // ---------- Valores ----------

    private static FilaHoja CrearFila(IXLWorksheet hoja, int fila, string cedula, Dictionary<string, int> columnas)
    {
        string Leer(string campo) => columnas.TryGetValue(campo, out var columna) ? Texto(hoja.Cell(fila, columna)).Trim() : string.Empty;

        return new FilaHoja
        {
            NumeroFila = fila,
            Cedula = cedula,
            NombreCompleto = $"{Leer(Nombre)} {Leer(Apellidos)}".Trim().Replace("  ", " "),
            Direccion = Leer(Direccion),
            Barrio = Leer(Barrio),
            Celular = columnas.TryGetValue(Celular, out var columnaCelular) ? LimpiarNumero(hoja.Cell(fila, columnaCelular)) : string.Empty
        };
    }

    private static string Texto(IXLCell celda) => celda.IsEmpty() ? string.Empty : celda.GetFormattedString();

    private static string Normalizar(string texto)
    {
        var descompuesto = texto.Trim().ToUpperInvariant().Normalize(System.Text.NormalizationForm.FormD);
        return new string(descompuesto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }

    /// <summary>Lee una cédula o celular: números de Excel sin decimales y textos con puntos o espacios de separación.</summary>
    private static string LimpiarNumero(IXLCell celda)
    {
        if (celda.IsEmpty())
        {
            return string.Empty;
        }

        if (celda.DataType == XLDataType.Number)
        {
            return Math.Round(celda.GetDouble()).ToString("0", CultureInfo.InvariantCulture);
        }

        var texto = celda.GetString().Trim();
        return Regex.IsMatch(texto, @"^[\d.\s,]+$") ? Regex.Replace(texto, @"\D", string.Empty) : texto;
    }

    private static bool TryLeerFecha(IXLCell celda, out DateOnly fecha)
    {
        fecha = default;
        if (celda.DataType == XLDataType.DateTime)
        {
            fecha = DateOnly.FromDateTime(celda.GetDateTime());
            return true;
        }

        if (celda.DataType == XLDataType.Number)
        {
            fecha = DateOnly.FromDateTime(DateTime.FromOADate(celda.GetDouble()));
            return true;
        }

        return DateOnly.TryParseExact(celda.GetString().Trim(), FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
    }

    private static bool TryLeerHora(IXLCell celda, out TimeOnly hora)
    {
        hora = default;
        if (celda.IsEmpty())
        {
            return false;
        }

        if (celda.DataType == XLDataType.TimeSpan)
        {
            hora = TimeOnly.FromTimeSpan(celda.GetTimeSpan());
            return true;
        }

        if (celda.DataType == XLDataType.DateTime)
        {
            hora = TimeOnly.FromDateTime(celda.GetDateTime());
            return true;
        }

        if (celda.DataType == XLDataType.Number)
        {
            var valor = celda.GetDouble();
            if (valor < 1)
            {
                hora = TimeOnly.FromTimeSpan(TimeSpan.FromDays(valor));
                return true;
            }

            if (valor < 24 && valor == Math.Floor(valor))
            {
                hora = new TimeOnly((int)valor, 0);
                return true;
            }

            var comoTexto = ((int)valor).ToString("0000", CultureInfo.InvariantCulture);
            return TryLeerHoraTexto(comoTexto, out hora);
        }

        return TryLeerHoraTexto(celda.GetString(), out hora);
    }

    /// <summary>Acepta "4AM", "4 a.m.", "4:30 pm", "16:00", "16:00:00", "1600" y "4".</summary>
    internal static bool TryLeerHoraTexto(string texto, out TimeOnly hora)
    {
        hora = default;
        texto = texto.Trim();

        var meridiano = HoraConMeridiano.Match(texto);
        if (meridiano.Success)
        {
            var horas = int.Parse(meridiano.Groups[1].Value, CultureInfo.InvariantCulture);
            var minutos = meridiano.Groups[2].Success ? int.Parse(meridiano.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
            if (horas is < 1 or > 12 || minutos > 59)
            {
                return false;
            }

            var esPm = char.ToUpperInvariant(meridiano.Groups[3].Value[0]) == 'P';
            hora = new TimeOnly(horas % 12 + (esPm ? 12 : 0), minutos);
            return true;
        }

        var cuatro = HoraCuatroDigitos.Match(texto);
        if (cuatro.Success)
        {
            var horas = int.Parse(cuatro.Groups[1].Value, CultureInfo.InvariantCulture);
            var minutos = int.Parse(cuatro.Groups[2].Value, CultureInfo.InvariantCulture);
            if (horas > 23 || minutos > 59)
            {
                return false;
            }

            hora = new TimeOnly(horas, minutos);
            return true;
        }

        if (int.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out var soloHora) && soloHora < 24)
        {
            hora = new TimeOnly(soloHora, 0);
            return true;
        }

        return TimeOnly.TryParse(texto, CultureInfo.InvariantCulture, out hora);
    }
}
