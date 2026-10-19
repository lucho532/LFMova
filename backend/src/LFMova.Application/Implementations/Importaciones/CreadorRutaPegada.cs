using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.DTOs.Jornadas;
using LFMova.Application.DTOs.Programaciones;
using LFMova.Application.DTOs.Servicios;
using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Application.Validators;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Crea una ruta nueva con los pasajeros que el coordinador pegó desde un Excel externo: abre el
/// servicio, asegura a cada empleado y su programación y lo asigna como pasajero. No interpreta el
/// pegado (eso ya lo resolvió el frontend) ni decide autorización.
/// </summary>
public class CreadorRutaPegada
{
    private readonly IJornadaServicio _jornadaServicio;
    private readonly IServicioServicio _servicioServicio;
    private readonly IServicioPasajeroServicio _servicioPasajeroServicio;
    private readonly IProgramacionTransporteServicio _programacionServicio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly INotificacionServicio _notificacionServicio;
    private readonly AseguradorEmpleadoImportacion _aseguradorEmpleado;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public CreadorRutaPegada(
        IJornadaServicio jornadaServicio,
        IServicioServicio servicioServicio,
        IServicioPasajeroServicio servicioPasajeroServicio,
        IProgramacionTransporteServicio programacionServicio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        INotificacionServicio notificacionServicio,
        AseguradorEmpleadoImportacion aseguradorEmpleado)
    {
        _jornadaServicio = jornadaServicio;
        _servicioServicio = servicioServicio;
        _servicioPasajeroServicio = servicioPasajeroServicio;
        _programacionServicio = programacionServicio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _notificacionServicio = notificacionServicio;
        _aseguradorEmpleado = aseguradorEmpleado;
    }

    /// <summary>
    /// Crea la ruta y le agrega los pasajeros pegados. Las filas inválidas o repetidas se omiten con un
    /// aviso; si ninguna pudo agregarse, la ruta recién creada se elimina y se informa como fallo.
    /// </summary>
    public async Task<ResultadoImportacionDto> CrearAsync(int empresaId, CrearRutaPegadaDto datos)
    {
        if (datos.Pasajeros.Count == 0)
        {
            throw new InvalidOperationException("No hay ningún pasajero para crear la ruta.");
        }

        // A diferencia de CreadorRutaVacia, acá siempre se crea una ruta nueva (nunca se reutiliza una
        // existente): cada pegado del coordinador representa explícitamente una tarjeta nueva. Si no se
        // indica unidad, queda sin conductor (PENDIENTE_ASIGNACION) para asignárselo después desde ahí
        // mismo, igual que una ruta importada que no alcanzó a repartirse por falta de unidades; si sí
        // se indica, queda asignada de una vez (CrearAsync ya valida que la unidad esté disponible para
        // ese horario).
        var jornadas = await _jornadaServicio.ObtenerPorEmpresaAsync(empresaId);
        var jornada = jornadas.FirstOrDefault(j => j.FechaOperativa == datos.Fecha)
            ?? await _jornadaServicio.CrearAsync(empresaId, new CrearJornadaDto { FechaOperativa = datos.Fecha });

        var servicio = await _servicioServicio.CrearAsync(empresaId, jornada.JornadaId, new CrearServicioDto
        {
            SedeId = datos.SedeId,
            Fecha = datos.Fecha,
            HoraProgramada = datos.Hora,
            Tipo = datos.Tipo,
            UnidadOperativaId = datos.UnidadOperativaId
        });
        await _servicioServicio.CambiarEstadoAsync(empresaId, servicio.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
        if (datos.UnidadOperativaId is not null)
        {
            await _servicioServicio.CambiarEstadoAsync(empresaId, servicio.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.ASIGNADO });
        }
        servicio = await _servicioServicio.ObtenerPorIdAsync(empresaId, servicio.ServicioId);

        var resultado = new ResultadoImportacionDto { JornadaId = jornada.JornadaId, ServicioId = servicio!.ServicioId, ServiciosCreados = 1 };

        // Filas inválidas o con una cédula repetida dentro del mismo pegado no abortan toda la creación:
        // se omiten con un aviso (mismo criterio que la importación por Excel) y el resto sí se agrega.
        var cedulasYaProcesadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var indice = 0; indice < datos.Pasajeros.Count; indice++)
        {
            var pasajero = datos.Pasajeros[indice];
            try
            {
                ValidarDatosDePasajero(pasajero.Cedula, pasajero.NombreCompleto, pasajero.Direccion, pasajero.Barrio);
                if (!cedulasYaProcesadas.Add(pasajero.Cedula.Trim()))
                {
                    resultado.FilasOmitidas++;
                    resultado.Advertencias.Add($"Fila {indice + 1}: la cédula {pasajero.Cedula.Trim()} está repetida en el pegado, se omitió.");
                    continue;
                }

                var fila = new FilaHoja
                {
                    Cedula = pasajero.Cedula.Trim(),
                    NombreCompleto = pasajero.NombreCompleto.Trim(),
                    Celular = pasajero.Celular.Trim(),
                    Direccion = pasajero.Direccion.Trim(),
                    Barrio = pasajero.Barrio.Trim()
                };
                // Si se indicó unidad, todos estos pasajeros son parte de la ruta recién creada (no avisos
                // de "pasajero de última hora" uno por uno): si el conductor necesita enterarse de su ruta
                // nueva, eso se resuelve aparte, no con N notificaciones repetidas.
                await AgregarPasajeroAlServicioAsync(empresaId, servicio, fila, resultado, notificarConductorSiTieneUnidad: datos.UnidadOperativaId is null);
            }
            catch (InvalidOperationException excepcion)
            {
                resultado.FilasOmitidas++;
                resultado.Advertencias.Add($"Fila {indice + 1}: {excepcion.Message}");
            }
        }

        // Si ninguna fila pudo agregarse (todas fallaron o venían repetidas), no queda una ruta vacía
        // esperando pasajeros: se elimina la que se acababa de crear y se informa como fallo, no como éxito.
        if (resultado.PasajerosAsignados == 0)
        {
            await _servicioServicio.EliminarAsync(empresaId, servicio.ServicioId);
            throw new InvalidOperationException(
                resultado.Advertencias.Count > 0 ? string.Join(" ", resultado.Advertencias) : "No se pudo agregar a ningún pasajero.");
        }

        return resultado;
    }

    private static void ValidarDatosDePasajero(string cedula, string nombreCompleto, string direccion, string barrio)
    {
        if (!CedulaValidador.EsValida(cedula))
        {
            throw new InvalidOperationException("La cédula es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(nombreCompleto) || string.IsNullOrWhiteSpace(direccion) || string.IsNullOrWhiteSpace(barrio))
        {
            throw new InvalidOperationException("El nombre, la dirección y el barrio son obligatorios.");
        }
    }

    /// <summary>
    /// Asegura al empleado (lo crea o lo vincula) y su programación de
    /// transporte, y lo asigna como pasajero del servicio indicado. Falla si
    /// esa persona ya está asignada a una ruta de ese mismo horario.
    /// </summary>
    private async Task AgregarPasajeroAlServicioAsync(int empresaId, ServicioDto servicio, FilaHoja fila, ResultadoImportacionDto resultado, bool notificarConductorSiTieneUnidad = true)
    {
        var empleado = await _aseguradorEmpleado.AsegurarAsync(empresaId, fila, resultado);

        var programaciones = await _programacionServicio.ObtenerPorEmpresaAsync(empresaId);
        var programacion = programaciones.FirstOrDefault(p =>
            p.EmpleadoId == empleado.EmpleadoId && p.SedeId == servicio.SedeId && p.Fecha == servicio.Fecha && p.Hora == servicio.HoraProgramada && p.Tipo == servicio.Tipo);

        if (programacion is not null && await _servicioPasajeroRepositorio.ObtenerPorProgramacionAsync(programacion.ProgramacionTransporteId) is not null)
        {
            throw new InvalidOperationException("Esa persona ya está asignada a una ruta de ese horario.");
        }

        programacion ??= await _programacionServicio.CrearAsync(empresaId, new CrearProgramacionDto
        {
            EmpleadoId = empleado.EmpleadoId,
            SedeId = servicio.SedeId,
            Fecha = servicio.Fecha,
            Hora = servicio.HoraProgramada,
            Tipo = servicio.Tipo,
            DireccionRecogida = fila.Direccion,
            BarrioRecogida = fila.Barrio
        });
        resultado.ProgramacionesCreadas++;

        var pasajeroCreado = await _servicioPasajeroServicio.CrearAsync(
            empresaId, servicio.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = programacion.ProgramacionTransporteId });
        resultado.PasajerosAsignados++;

        // Si la ruta ya tenía conductor, agregar un pasajero de última hora la modifica: se le avisa para
        // que no se entere solo al llegar y encontrar una parada que no esperaba. No aplica cuando el
        // pasajero se agrega como parte de la creación inicial de la ruta (ver notificarConductorSiTieneUnidad):
        // ahí no es "de última hora", es la ruta completa recién armada.
        if (servicio.UnidadOperativaId is not null && notificarConductorSiTieneUnidad
            && ReglasEstadoServicio.EsVisibleParaConductorYEmpleado(servicio.Estado))
        {
            var usuarioIdConductor = await _servicioPasajeroServicio.ObtenerUsuarioIdConductorAsync(empresaId, pasajeroCreado.ServicioPasajeroId);
            if (usuarioIdConductor is not null)
            {
                await _notificacionServicio.CrearAsync(
                    usuarioIdConductor.Value,
                    "RUTA_MODIFICADA",
                    "Tu ruta cambió",
                    $"Se agregó un pasajero de última hora a tu {FormatoOperacion.DescribirRuta(servicio.Tipo, servicio.HoraProgramada, servicio.Fecha, servicio.NombreSede)}.");
            }
        }
    }
}
