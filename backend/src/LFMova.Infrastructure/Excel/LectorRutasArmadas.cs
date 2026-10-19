using ClosedXML.Excel;
using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Utils;
using LFMova.Domain.Enums;
using static LFMova.Infrastructure.Excel.CamposHoja;
using static LFMova.Infrastructure.Excel.CeldasHoja;
using static LFMova.Infrastructure.Excel.ColumnasRutasArmadas;

namespace LFMova.Infrastructure.Excel;

/// <summary>
/// Recorre una hoja con rutas ya armadas y reúne los pasajeros de cada ruta tal como vienen, en el orden
/// de las filas: por el número de ruta de cada fila (un bloque seguido de filas con el mismo número es una ruta; el
/// mismo número en otro bloque es otra ruta) o por la línea de texto que abre cada ruta. Ignora las
/// líneas de nombre de conductor, los encabezados repetidos y las filas sueltas. No reparte ni reordena.
/// </summary>
internal sealed class LectorRutasArmadas
{
    private readonly IXLWorksheet _hoja;
    private readonly Dictionary<string, int> _columnas;
    private readonly ModoSeparacionRutas _modo;
    private readonly string _marcador;
    private readonly HojaRutasArmadas _resultado = new();
    private readonly Dictionary<string, int> _vecesPorNumero = new();
    private readonly List<RutaEnLectura> _rutas = new();
    private RutaEnLectura? _actual;
    private string _numeroActual = string.Empty;
    private DateOnly? _fechaActual;
    private bool _hayCorte;
    private bool _avisoSinLinea;

    private LectorRutasArmadas(IXLWorksheet hoja, Dictionary<string, int> columnas, ModoSeparacionRutas modo, string marcador)
    {
        _hoja = hoja;
        _columnas = columnas;
        _modo = modo;
        _marcador = TextoNormalizador.Normalizar(marcador);
    }

    internal static HojaRutasArmadas Leer(IXLWorksheet hoja, ModoSeparacionRutas modo, string? textoInicioRuta)
    {
        var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 0;
        var ultimaColumna = Math.Max(hoja.LastColumnUsed()?.ColumnNumber() ?? 0, 8);
        var columnas = Buscar(hoja, ultimaFila, ultimaColumna);
        var faltantes = Faltantes(columnas, modo);
        if (faltantes.Length > 0)
        {
            return new HojaRutasArmadas { Errores = { $"No se encontraron las columnas necesarias. Faltan: {faltantes}." } };
        }

        var lector = new LectorRutasArmadas(hoja, columnas, modo, textoInicioRuta ?? string.Empty);
        for (var fila = 1; fila <= ultimaFila; fila++)
        {
            lector.LeerFila(fila, ultimaColumna);
        }

        return lector.Terminar();
    }

    private void LeerFila(int fila, int ultimaColumna)
    {
        if (!EsPasajero(_hoja, fila, _columnas, out var cedula))
        {
            if (_modo == ModoSeparacionRutas.LINEA_INICIO)
            {
                AbrirRutaSiEsLineaDeInicio(fila, ultimaColumna);
            }
            else if (!_hoja.Row(fila).IsEmpty())
            {
                // Una línea que no es de pasajero (nombre del conductor, encabezados repetidos) cierra el bloque: lo que siga es otra ruta.
                _hayCorte = true;
            }

            return;
        }

        if (!TryLeerHoraYTipo(_hoja, fila, _columnas, out var horario, out var errorHora))
        {
            _resultado.Errores.Add(errorHora!);
            return;
        }

        DateOnly? fecha = null;
        var textoFecha = LeerTexto(_hoja, fila, _columnas, Fecha);
        if (textoFecha.Length > 0)
        {
            if (!TryLeerFecha(_hoja.Cell(fila, _columnas[Fecha]), out var leida))
            {
                _resultado.Errores.Add($"Fila {fila}: la fecha \"{textoFecha}\" no es válida.");
                return;
            }

            fecha = leida;
        }

        var ruta = ElegirRuta(fila, fecha);
        if (ruta is null)
        {
            return;
        }

        ruta.AgregarDatos(fila, horario, fecha, LeerTexto(_hoja, fila, _columnas, Sede), LeerTexto(_hoja, fila, _columnas, Placa), _resultado);
        ruta.Ruta.Filas.Add(CrearFilaCorregida(fila, cedula));
    }

    private RutaEnLectura? ElegirRuta(int fila, DateOnly? fecha)
    {
        if (_modo == ModoSeparacionRutas.LINEA_INICIO)
        {
            if (_actual is null && !_avisoSinLinea)
            {
                _avisoSinLinea = true;
                _resultado.Errores.Add($"Fila {fila}: hay pasajeros antes de la primera línea que abre una ruta (la que empieza con \"{_marcador}\").");
            }

            return _actual;
        }

        var numero = LeerTexto(_hoja, fila, _columnas, NumeroRuta);
        if (numero.Length == 0)
        {
            _resultado.Errores.Add($"Fila {fila}: no tiene número de ruta.");
            return null;
        }

        // Cada bloque es su propia ruta: el mismo número en otro bloque (rutas dobles de un mismo vehículo, que el
        // conductor cobra por separado) no se une, aunque el número y la placa sean iguales.
        if (_actual is null || _hayCorte || numero != _numeroActual || fecha != _fechaActual)
        {
            _vecesPorNumero[numero] = _vecesPorNumero.GetValueOrDefault(numero) + 1;
            var veces = _vecesPorNumero[numero];
            _actual = new RutaEnLectura(veces == 1 ? numero : $"{numero} (bloque {veces})");
            _rutas.Add(_actual);
            (_numeroActual, _fechaActual, _hayCorte) = (numero, fecha, false);
        }

        return _actual;
    }

    private void AbrirRutaSiEsLineaDeInicio(int fila, int ultimaColumna)
    {
        var textos = Enumerable.Range(1, ultimaColumna).Select(c => Texto(_hoja.Cell(fila, c)).Trim()).Where(t => t.Length > 0).ToList();
        if (_marcador.Length == 0 || textos.Count == 0 || !TextoNormalizador.Normalizar(textos[0]).StartsWith(_marcador, StringComparison.Ordinal))
        {
            return;
        }

        var linea = string.Join(" ", textos);
        _actual = new RutaEnLectura(linea, InterpretadorLineaInicioRuta.Interpretar(linea));
        _rutas.Add(_actual);
    }

    private FilaHoja CrearFilaCorregida(int fila, string cedula)
    {
        var pasajero = CrearFila(_hoja, fila, cedula, _columnas);
        pasajero.NombreCompleto = CorrectorTextoDanado.Corregir(pasajero.NombreCompleto, out var a);
        pasajero.Direccion = CorrectorTextoDanado.Corregir(pasajero.Direccion, out var b);
        pasajero.Barrio = CorrectorTextoDanado.Corregir(pasajero.Barrio, out var c);
        if (a || b || c)
        {
            _resultado.Advertencias.Add($"Fila {fila}: el nombre, la dirección o el barrio de {pasajero.NombreCompleto} tiene caracteres dañados que no se pudieron corregir; revísalo.");
        }

        return pasajero;
    }

    private HojaRutasArmadas Terminar()
    {
        foreach (var ruta in _rutas)
        {
            if (ruta.Ruta.Filas.Count == 0)
            {
                _resultado.Advertencias.Add($"La ruta \"{ruta.Ruta.Referencia}\" no tiene pasajeros: se omitió.");
            }
            else if (ruta.CerrarYValidar(_resultado))
            {
                _resultado.Rutas.Add(ruta.Ruta);
            }
        }

        if (_resultado.Rutas.Count == 0 && _resultado.Errores.Count == 0)
        {
            _resultado.Errores.Add(_modo == ModoSeparacionRutas.LINEA_INICIO
                ? $"No se encontró ninguna línea que empiece con \"{_marcador}\" seguida de pasajeros."
                : "El archivo no contiene ningún pasajero.");
        }

        return _resultado;
    }
}
