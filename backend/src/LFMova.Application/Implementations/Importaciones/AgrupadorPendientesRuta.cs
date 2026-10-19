using LFMova.Application.DTOs.Programaciones;
using LFMova.Application.Interfaces;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Primera pasada de la importación: por cada fila de la hoja asegura al empleado y su programación
/// de transporte, y reúne por ruta real a quienes todavía no son pasajeros de ningún servicio. No
/// crea servicios ni decide qué unidad lleva a cada persona.
/// </summary>
public class AgrupadorPendientesRuta
{
    private static readonly TimeOnly LimiteMadrugada = new(12, 0);

    private readonly IProgramacionTransporteServicio _programacionServicio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly ResolutorSedeImportacion _resolutorSede;
    private readonly AseguradorEmpleadoImportacion _aseguradorEmpleado;
    private readonly CargadorJornadasImportacion _cargadorJornadas;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public AgrupadorPendientesRuta(
        IProgramacionTransporteServicio programacionServicio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        ResolutorSedeImportacion resolutorSede,
        AseguradorEmpleadoImportacion aseguradorEmpleado,
        CargadorJornadasImportacion cargadorJornadas)
    {
        _programacionServicio = programacionServicio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _resolutorSede = resolutorSede;
        _aseguradorEmpleado = aseguradorEmpleado;
        _cargadorJornadas = cargadorJornadas;
    }

    /// <summary>
    /// Arma los pendientes de cada bloque del Excel y los agrupa por ruta real
    /// (jornada + sede + tipo + fecha + hora). Así, si la hoja trae la misma ruta partida en varios
    /// bloques (por ejemplo, filas de otro horario intercaladas, o una fecha en unas filas y no en
    /// otras que igual cae en el mismo día), todos sus pasajeros se reparten juntos en vez de abrir
    /// una ruta nueva y agotar unidades por cada bloque.
    /// </summary>
    public async Task<Dictionary<ClaveRuta, RutaPendiente>> AgruparAsync(
        ContextoImportacion contexto, AnalisisImportacion analisis, DateOnly fechaOperativa, int? sedeGeneralId)
    {
        var empresaId = contexto.EmpresaId;
        var resultado = contexto.Resultado;
        var rutasPendientes = new Dictionary<ClaveRuta, RutaPendiente>();
        var programacionesYaPendientes = new HashSet<int>();

        foreach (var grupo in analisis.Hoja.Grupos)
        {
            var sede = await _resolutorSede.ObtenerOCrearAsync(empresaId, grupo.NombreSede, sedeGeneralId, contexto.Sedes, resultado);
            var fechaServicio = grupo.Fecha ?? (analisis.Vista.CruzaMedianoche && grupo.Hora < LimiteMadrugada ? fechaOperativa.AddDays(1) : fechaOperativa);
            var claveJornada = grupo.Fecha ?? fechaOperativa;
            await _cargadorJornadas.ObtenerAsync(contexto, claveJornada);

            var pendientesDelGrupo = new List<Pendiente>();
            foreach (var fila in grupo.Filas)
            {
                var empleado = await _aseguradorEmpleado.AsegurarAsync(empresaId, fila, resultado);

                var programacion = contexto.Programaciones.FirstOrDefault(p =>
                    p.EmpleadoId == empleado.EmpleadoId && p.SedeId == sede.SedeId && p.Fecha == fechaServicio && p.Hora == grupo.Hora && p.Tipo == grupo.Tipo);

                if (programacion is null)
                {
                    programacion = await _programacionServicio.CrearAsync(empresaId, new CrearProgramacionDto
                    {
                        EmpleadoId = empleado.EmpleadoId,
                        SedeId = sede.SedeId,
                        Fecha = fechaServicio,
                        Hora = grupo.Hora,
                        Tipo = grupo.Tipo,
                        DireccionRecogida = fila.Direccion,
                        BarrioRecogida = fila.Barrio
                    });
                    contexto.Programaciones.Add(programacion);
                    resultado.ProgramacionesCreadas++;
                }

                if (!programacionesYaPendientes.Add(programacion.ProgramacionTransporteId))
                {
                    resultado.FilasOmitidas++;
                    continue;
                }

                if (await _servicioPasajeroRepositorio.ObtenerPorProgramacionAsync(programacion.ProgramacionTransporteId) is not null)
                {
                    resultado.FilasOmitidas++;
                    continue;
                }

                pendientesDelGrupo.Add(new Pendiente(fila, programacion));
            }

            if (pendientesDelGrupo.Count == 0)
            {
                continue;
            }

            var clave = new ClaveRuta(claveJornada, fechaServicio, sede.SedeId, grupo.Tipo, grupo.Hora);
            if (!rutasPendientes.TryGetValue(clave, out var ruta))
            {
                ruta = new RutaPendiente(sede, grupo.Titulo, new List<Pendiente>());
                rutasPendientes[clave] = ruta;
            }

            ruta.Pendientes.AddRange(pendientesDelGrupo);
        }

        return rutasPendientes;
    }
}
