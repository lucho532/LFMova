using System.Text.RegularExpressions;
using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Interpreta el archivo de programación y lo compara con los datos de la empresa para armar la vista
/// previa: qué servicios trae, qué sedes existen, qué pasaría con cada persona y qué errores impiden
/// importar. No guarda nada: la importación en sí la hace <see cref="EjecutorImportacion"/>.
/// </summary>
public class AnalizadorImportacion
{
    private static readonly Regex RangoDeDias = new(@"\d{1,2}\s*[-–/]\s*\d{1,2}", RegexOptions.Compiled);

    private readonly IExcelProgramacionLector _lector;
    private readonly IZonaRepositorio _zonaRepositorio;
    private readonly ResolutorSedeImportacion _resolutorSede;
    private readonly AseguradorEmpleadoImportacion _aseguradorEmpleado;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public AnalizadorImportacion(
        IExcelProgramacionLector lector,
        IZonaRepositorio zonaRepositorio,
        ResolutorSedeImportacion resolutorSede,
        AseguradorEmpleadoImportacion aseguradorEmpleado)
    {
        _lector = lector;
        _zonaRepositorio = zonaRepositorio;
        _resolutorSede = resolutorSede;
        _aseguradorEmpleado = aseguradorEmpleado;
    }

    /// <summary>
    /// Lee el archivo y arma su vista previa para la empresa. Falla solo si el archivo no es un Excel
    /// legible; los problemas de contenido quedan en los errores y advertencias de la vista.
    /// </summary>
    public async Task<AnalisisImportacion> AnalizarAsync(int empresaId, Stream archivo)
    {
        HojaProgramacion hoja;
        try
        {
            hoja = _lector.Leer(archivo);
        }
        catch (Exception excepcion) when (excepcion is not InvalidOperationException)
        {
            throw new InvalidOperationException("El archivo no es un Excel válido (.xlsx).");
        }

        var vista = new VistaPreviaImportacionDto
        {
            Transportador = hoja.Transportador,
            FechaTexto = hoja.FechaTexto,
            CruzaMedianoche = !hoja.FechasPorFila && RangoDeDias.IsMatch(hoja.FechaTexto),
            FechaOperativaSugerida = hoja.Grupos.Where(g => g.Fecha is not null).Select(g => g.Fecha).Min()
        };
        vista.Errores.AddRange(hoja.Errores);

        var sedes = await _resolutorSede.ObtenerActivasAsync(empresaId);
        var sedePorGrupo = new List<Sede?>();
        var situaciones = new Dictionary<string, string>();
        var cedulasNuevas = new HashSet<string>();

        // Se agrupan los bloques del Excel por ruta real (tipo + sede + hora), igual que al importar: si la
        // hoja trae la misma ruta partida en varios bloques (por ejemplo, uno sin fecha en la celda y otro
        // con la fecha explícita, que terminan siendo el mismo día), se muestra un solo cuadro en vez de uno
        // por bloque. Solo se abre un cuadro nuevo para el mismo tipo+sede+hora cuando dos bloques traen
        // fechas explícitas y distintas (la hoja realmente cubre más de un día).
        var candidatosPorClave = new Dictionary<(TipoServicio Tipo, string SedeClave, TimeOnly Hora), List<ServicioPreviaDto>>();

        foreach (var grupo in hoja.Grupos)
        {
            var sede = ResolutorSedeImportacion.Buscar(sedes, grupo.NombreSede);
            sedePorGrupo.Add(sede);

            if (sede is null)
            {
                var mensaje = grupo.NombreSede.Trim().Length == 0
                    ? "El archivo no trae la sede de estos pasajeros: elige una sede al importar."
                    : $"La sede \"{grupo.NombreSede}\" no existe todavía: se creará automáticamente al importar.";
                if (!vista.Advertencias.Contains(mensaje))
                {
                    vista.Advertencias.Add(mensaje);
                }
            }

            var pasajerosDelGrupo = new List<PasajeroPreviaDto>();
            var cedulasDelGrupo = new HashSet<string>();

            foreach (var fila in grupo.Filas)
            {
                if (!cedulasDelGrupo.Add(fila.Cedula))
                {
                    vista.Errores.Add($"Fila {fila.NumeroFila}: la cédula {fila.Cedula} está repetida en el mismo servicio.");
                }

                if (string.IsNullOrWhiteSpace(fila.Direccion) || string.IsNullOrWhiteSpace(fila.Barrio))
                {
                    vista.Errores.Add($"Fila {fila.NumeroFila}: falta la dirección o el barrio.");
                }

                if (string.IsNullOrWhiteSpace(fila.Celular))
                {
                    vista.Advertencias.Add($"Fila {fila.NumeroFila}: falta el celular.");
                }

                if (!situaciones.TryGetValue(fila.Cedula, out var situacion))
                {
                    situacion = await _aseguradorEmpleado.ClasificarAsync(empresaId, fila.Cedula);
                    situaciones[fila.Cedula] = situacion;

                    if (situacion == "NUEVO")
                    {
                        cedulasNuevas.Add(fila.Cedula);
                    }
                    else if (situacion == "CAMBIA_DE_EMPRESA")
                    {
                        vista.Advertencias.Add($"{fila.NombreCompleto} (cédula {fila.Cedula}) pertenece a otra empresa y pasará a esta al importar.");
                    }
                }

                pasajerosDelGrupo.Add(new PasajeroPreviaDto
                {
                    Cedula = fila.Cedula,
                    NombreCompleto = fila.NombreCompleto,
                    Direccion = fila.Direccion,
                    Barrio = fila.Barrio,
                    Celular = fila.Celular,
                    Situacion = situacion
                });
            }

            var claveSede = sede is not null ? $"id:{sede.SedeId}" : $"txt:{TextoNormalizador.Normalizar(grupo.NombreSede)}";
            var clave = (grupo.Tipo, claveSede, grupo.Hora);
            if (!candidatosPorClave.TryGetValue(clave, out var candidatos))
            {
                candidatos = new List<ServicioPreviaDto>();
                candidatosPorClave[clave] = candidatos;
            }

            var existente = candidatos.FirstOrDefault(s => s.Fecha is null || grupo.Fecha is null || s.Fecha == grupo.Fecha);
            if (existente is not null)
            {
                existente.Fecha ??= grupo.Fecha;
                existente.Pasajeros.AddRange(pasajerosDelGrupo);
            }
            else
            {
                var nuevo = new ServicioPreviaDto
                {
                    Tipo = grupo.Tipo,
                    SedeEnHoja = grupo.NombreSede,
                    SedeEncontrada = sede?.Nombre,
                    SedeId = sede?.SedeId,
                    Hora = grupo.Hora,
                    Fecha = grupo.Fecha,
                    Pasajeros = pasajerosDelGrupo
                };
                candidatos.Add(nuevo);
                vista.Servicios.Add(nuevo);
            }
        }

        vista.TotalPasajeros = vista.Servicios.Sum(s => s.Pasajeros.Count);
        vista.EmpleadosNuevos = cedulasNuevas.Count;
        // Del primer horario al último, para que el coordinador los revise en el orden en que ocurren.
        vista.Servicios = vista.Servicios.OrderBy(s => s.Hora).ThenBy(s => s.Fecha).ToList();
        // Se calcula siempre al validar o importar, aunque el coordinador no haya elegido repartir entre
        // unidades esta vez.
        var mapaZonas = MapaZonas.Crear(await _zonaRepositorio.ObtenerPorEmpresaAsync(empresaId));
        vista.BarriosSinZona = mapaZonas.ObtenerBarriosSinZona(vista.Servicios.SelectMany(s => s.Pasajeros).Select(p => (p.Barrio, p.Direccion)));

        return new AnalisisImportacion(hoja, vista, sedePorGrupo);
    }
}
