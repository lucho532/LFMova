using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Resultado de interpretar un archivo con rutas armadas contra los datos de la empresa: la hoja leída, la
/// vista previa para el coordinador y, por cada ruta, la unidad que corresponde a su placa (o <c>null</c>).
/// No guarda nada por sí mismo.
/// </summary>
public sealed record AnalisisRutasArmadas(HojaRutasArmadas Hoja, VistaPreviaRutasArmadasDto Vista, List<UnidadPorPlaca?> UnidadPorRuta);

/// <summary>
/// Interpreta un Excel con rutas ya armadas y arma su vista previa: las rutas tal como vienen, el conductor
/// que corresponde a cada placa y lo que conviene revisar. Nunca propone cambiar las rutas: solo informa
/// (capacidad excedida, placa desconocida, persona repetida). No guarda nada: la importación la hace
/// <see cref="EjecutorRutasArmadas"/>.
/// </summary>
public class AnalizadorRutasArmadas
{
    private readonly IExcelRutasArmadasLector _lector;
    private readonly ResolutorSedeImportacion _resolutorSede;
    private readonly ResolutorUnidadPorPlaca _resolutorUnidad;
    private readonly AseguradorEmpleadoImportacion _aseguradorEmpleado;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public AnalizadorRutasArmadas(
        IExcelRutasArmadasLector lector, ResolutorSedeImportacion resolutorSede, ResolutorUnidadPorPlaca resolutorUnidad, AseguradorEmpleadoImportacion aseguradorEmpleado)
    {
        _lector = lector;
        _resolutorSede = resolutorSede;
        _resolutorUnidad = resolutorUnidad;
        _aseguradorEmpleado = aseguradorEmpleado;
    }

    /// <summary>
    /// Lee el archivo y arma su vista previa. Falla solo si el archivo no es un Excel legible o falta el
    /// texto de inicio de ruta; los problemas de contenido quedan en los errores y advertencias de la vista.
    /// </summary>
    public async Task<AnalisisRutasArmadas> AnalizarAsync(int empresaId, Stream archivo, ModoSeparacionRutas modo, string? textoInicioRuta)
    {
        if (modo == ModoSeparacionRutas.LINEA_INICIO && string.IsNullOrWhiteSpace(textoInicioRuta))
        {
            throw new InvalidOperationException("Indica con qué texto empieza cada ruta.");
        }

        HojaRutasArmadas hoja;
        try
        {
            hoja = _lector.Leer(archivo, modo, textoInicioRuta?.Trim());
        }
        catch (Exception excepcion) when (excepcion is not InvalidOperationException)
        {
            throw new InvalidOperationException("El archivo no es un Excel válido (.xlsx).");
        }

        var vista = new VistaPreviaRutasArmadasDto();
        vista.Errores.AddRange(hoja.Errores);
        vista.Advertencias.AddRange(hoja.Advertencias);

        var sedes = await _resolutorSede.ObtenerActivasAsync(empresaId);
        var unidades = await _resolutorUnidad.CargarAsync(empresaId);
        var unidadPorRuta = new List<UnidadPorPlaca?>();
        var situaciones = new Dictionary<string, string>();
        var cedulasNuevas = new HashSet<string>();

        foreach (var ruta in hoja.Rutas)
        {
            var sede = ResolutorSedeImportacion.Buscar(sedes, ruta.NombreSede);
            var unidad = ruta.Placa.Length > 0 && unidades.TryGetValue(ResolutorUnidadPorPlaca.NormalizarPlaca(ruta.Placa), out var encontrada) ? encontrada : null;
            unidadPorRuta.Add(unidad);

            var previa = new RutaArmadaPreviaDto
            {
                Referencia = ruta.Referencia, Tipo = ruta.Tipo, Hora = ruta.Hora, Fecha = ruta.Fecha, SedeEnHoja = ruta.NombreSede,
                SedeEncontrada = sede?.Nombre, Placa = ruta.Placa, Conductor = unidad?.Conductor, NumeroInterno = unidad?.NumeroInterno, Capacidad = unidad?.Capacidad
            };
            AvisarSedeYUnidad(vista, ruta, sede, unidad);
            await AgregarPasajerosAsync(empresaId, vista, ruta, previa, situaciones, cedulasNuevas);
            vista.Rutas.Add(previa);
        }

        AvisarPersonasRepetidas(vista, hoja);
        vista.TotalPasajeros = vista.Rutas.Sum(r => r.Pasajeros.Count);
        vista.FechaOperativaSugerida = hoja.Rutas.Where(r => r.Fecha is not null).Select(r => r.Fecha).Min();
        vista.FaltaSede = hoja.Rutas.Any(r => r.NombreSede.Trim().Length == 0);
        return new AnalisisRutasArmadas(hoja, vista, unidadPorRuta);
    }

    private static void AvisarSedeYUnidad(VistaPreviaRutasArmadasDto vista, RutaArmadaHoja ruta, Sede? sede, UnidadPorPlaca? unidad)
    {
        if (sede is null && ruta.NombreSede.Trim().Length > 0)
        {
            var mensaje = $"La sede \"{ruta.NombreSede}\" no existe todavía: se creará automáticamente al importar.";
            if (!vista.Advertencias.Contains(mensaje))
            {
                vista.Advertencias.Add(mensaje);
            }
        }

        if (ruta.Placa.Length == 0)
        {
            vista.Advertencias.Add($"La ruta \"{ruta.Referencia}\" no trae placa: quedará sin conductor para que lo asignes.");
        }
        else if (unidad is null)
        {
            vista.Advertencias.Add($"La ruta \"{ruta.Referencia}\" trae la placa o número interno \"{ruta.Placa}\", que no corresponde a ningún vehículo activo de tus conductores (los números internos se registran en cada conductor, en Unidades operativas): quedará sin conductor para que lo asignes.");
        }
    }

    private async Task AgregarPasajerosAsync(
        int empresaId, VistaPreviaRutasArmadasDto vista, RutaArmadaHoja ruta, RutaArmadaPreviaDto previa, Dictionary<string, string> situaciones, HashSet<string> cedulasNuevas)
    {
        var cedulasDeLaRuta = new HashSet<string>();
        foreach (var fila in ruta.Filas)
        {
            if (!cedulasDeLaRuta.Add(fila.Cedula))
            {
                vista.Errores.Add($"Fila {fila.NumeroFila}: la cédula {fila.Cedula} está repetida en la ruta \"{ruta.Referencia}\".");
            }

            if (string.IsNullOrWhiteSpace(fila.Direccion) || string.IsNullOrWhiteSpace(fila.Barrio))
            {
                vista.Errores.Add($"Fila {fila.NumeroFila}: falta la dirección o el barrio.");
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

            previa.Pasajeros.Add(new PasajeroPreviaDto
            {
                Cedula = fila.Cedula, NombreCompleto = fila.NombreCompleto, Direccion = fila.Direccion, Barrio = fila.Barrio, Celular = fila.Celular, Situacion = situacion
            });
        }

        if (previa.Capacidad is > 0 && previa.Pasajeros.Count > previa.Capacidad)
        {
            vista.Advertencias.Add($"La ruta \"{ruta.Referencia}\" tiene {previa.Pasajeros.Count} pasajeros y el vehículo {ruta.Placa} tiene capacidad {previa.Capacidad}: se importa tal como viene.");
        }
    }

    /// <summary>Avisa de la persona que aparece en dos rutas del mismo día y horario: solo puede estar en una.</summary>
    private static void AvisarPersonasRepetidas(VistaPreviaRutasArmadasDto vista, HojaRutasArmadas hoja)
    {
        var vistas = new Dictionary<(string, DateOnly?, TimeOnly, TipoServicio), string>();
        foreach (var ruta in hoja.Rutas)
        {
            foreach (var fila in ruta.Filas)
            {
                var clave = (fila.Cedula, ruta.Fecha, ruta.Hora, ruta.Tipo);
                if (vistas.TryGetValue(clave, out var primera) && primera != ruta.Referencia)
                {
                    vista.Advertencias.Add($"{fila.NombreCompleto} (cédula {fila.Cedula}) aparece en las rutas \"{primera}\" y \"{ruta.Referencia}\" a la misma hora: solo se agregará a la primera.");
                }
                else
                {
                    vistas[clave] = ruta.Referencia;
                }
            }
        }
    }
}
