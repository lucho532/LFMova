using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TransportApp.Application.DTOs.Importaciones;
using TransportApp.Application.DTOs.Jornadas;
using TransportApp.Application.DTOs.Programaciones;
using TransportApp.Application.DTOs.Servicios;
using TransportApp.Application.DTOs.ServiciosPasajero;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Utils;
using TransportApp.Application.Validators;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Domain.Rules;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa la importación de la programación diaria de un conductor desde
/// un Excel. Reutiliza los servicios existentes (programaciones, jornadas,
/// servicios y pasajeros) para que se apliquen las mismas validaciones que en
/// el alta manual. Es repetible: si se importa dos veces el mismo archivo, lo
/// ya importado se reutiliza y no se duplica. No es una transacción única: si
/// falla a mitad, lo ya guardado se conserva y se puede volver a importar.
/// No decide autorización: eso se verifica en la capa de Api.
/// </summary>
public class ImportacionExcelServicio : IImportacionExcelServicio
{
    private static readonly TimeOnly LimiteMadrugada = new(12, 0);
    private static readonly Regex RangoDeDias = new(@"\d{1,2}\s*[-–/]\s*\d{1,2}", RegexOptions.Compiled);

    private readonly IExcelProgramacionLector _lector;
    private readonly ISedeRepositorio _sedeRepositorio;
    private readonly IZonaRepositorio _zonaRepositorio;
    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IUsuarioRolRepositorio _usuarioRolRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IJornadaServicio _jornadaServicio;
    private readonly IServicioServicio _servicioServicio;
    private readonly IServicioPasajeroServicio _servicioPasajeroServicio;
    private readonly IProgramacionTransporteServicio _programacionServicio;
    private readonly IImportacionExcelRepositorio _importacionRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadRepositorio;
    private readonly IVehiculoRepositorio _vehiculoRepositorio;
    private readonly INotificacionServicio _notificacionServicio;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public ImportacionExcelServicio(
        IExcelProgramacionLector lector,
        ISedeRepositorio sedeRepositorio,
        IZonaRepositorio zonaRepositorio,
        IUsuarioRepositorio usuarioRepositorio,
        IUsuarioRolRepositorio usuarioRolRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IJornadaServicio jornadaServicio,
        IServicioServicio servicioServicio,
        IServicioPasajeroServicio servicioPasajeroServicio,
        IProgramacionTransporteServicio programacionServicio,
        IImportacionExcelRepositorio importacionRepositorio,
        IConductorRepositorio conductorRepositorio,
        IUnidadOperativaRepositorio unidadRepositorio,
        IVehiculoRepositorio vehiculoRepositorio,
        INotificacionServicio notificacionServicio)
    {
        _conductorRepositorio = conductorRepositorio;
        _unidadRepositorio = unidadRepositorio;
        _vehiculoRepositorio = vehiculoRepositorio;
        _notificacionServicio = notificacionServicio;
        _lector = lector;
        _sedeRepositorio = sedeRepositorio;
        _zonaRepositorio = zonaRepositorio;
        _usuarioRepositorio = usuarioRepositorio;
        _usuarioRolRepositorio = usuarioRolRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _jornadaServicio = jornadaServicio;
        _servicioServicio = servicioServicio;
        _servicioPasajeroServicio = servicioPasajeroServicio;
        _programacionServicio = programacionServicio;
        _importacionRepositorio = importacionRepositorio;
    }

    private sealed record Analisis(HojaProgramacion Hoja, VistaPreviaImportacionDto Vista, List<Sede?> SedePorGrupo);

    /// <inheritdoc />
    public async Task<VistaPreviaImportacionDto> ValidarAsync(int empresaId, Stream archivo)
        => (await AnalizarAsync(empresaId, archivo)).Vista;

    /// <inheritdoc />
    public async Task<ResultadoImportacionDto> ImportarAsync(
        int empresaId, int usuarioCoordinadorId, string nombreArchivo, Stream archivo, DateOnly fechaOperativa, int? unidadOperativaId, bool repartirEntreUnidades, int? sedeGeneralId)
    {
        var analisis = await AnalizarAsync(empresaId, archivo);
        if (!analisis.Vista.PuedeImportar)
        {
            throw new InvalidOperationException("El archivo tiene errores y no se puede importar: " + string.Join(" ", analisis.Vista.Errores));
        }

        var resultado = new ResultadoImportacionDto
        {
            Advertencias = new List<string>(analisis.Vista.Advertencias),
            BarriosSinZona = new List<string>(analisis.Vista.BarriosSinZona)
        };

        try
        {
            await EjecutarAsync(empresaId, analisis, fechaOperativa, unidadOperativaId, repartirEntreUnidades, sedeGeneralId, resultado);
        }
        catch
        {
            await RegistrarAsync(empresaId, usuarioCoordinadorId, nombreArchivo, EstadoImportacionExcel.ERROR);
            throw;
        }

        await RegistrarAsync(
            empresaId, usuarioCoordinadorId, nombreArchivo,
            resultado.Advertencias.Count > 0 ? EstadoImportacionExcel.COMPLETADA_CON_ADVERTENCIAS : EstadoImportacionExcel.COMPLETADA);

        return resultado;
    }

    /// <inheritdoc />
    public async Task<ResultadoImportacionDto> CrearRutaVaciaAsync(int empresaId, CrearRutaVaciaDto datos)
    {
        // Sirve para dividir una ruta sobrecargada: primero se abre esta ruta vacía para un conductor
        // libre en el mismo horario, y luego se le mueven pasajeros desde la ruta original (ver
        // ServicioPasajeroServicio.MoverAsync), sin tener que dar de alta a nadie por acá.
        var (resultado, _) = await ObtenerOCrearRutaAsignadaAsync(
            empresaId, datos.Fecha, datos.Hora, datos.Tipo, datos.SedeId, datos.UnidadOperativaId,
            "No se puede usar una ruta cancelada, en curso o finalizada.");
        return resultado;
    }

    /// <inheritdoc />
    public async Task<ResultadoImportacionDto> CrearRutaPegadaAsync(int empresaId, CrearRutaPegadaDto datos)
    {
        if (datos.Pasajeros.Count == 0)
        {
            throw new InvalidOperationException("No hay ningún pasajero para crear la ruta.");
        }

        // A diferencia de CrearRutaManualAsync/CrearRutaVaciaAsync, acá siempre se crea una ruta nueva
        // (nunca se reutiliza una existente): cada pegado del coordinador representa explícitamente una
        // tarjeta nueva. Si no se indica unidad, queda sin conductor (PENDIENTE_ASIGNACION) para
        // asignárselo después desde ahí mismo, igual que una ruta importada que no alcanzó a repartirse
        // por falta de unidades; si sí se indica, queda asignada de una vez (CrearAsync ya valida que la
        // unidad esté disponible para ese horario).
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

    /// <summary>
    /// Reutiliza la ruta que ya tenga esa unidad en ese mismo horario, sede y tipo (dentro de la jornada
    /// de esa fecha, que también se reutiliza o se crea), o crea una nueva y la pasa directo a
    /// <see cref="EstadoServicio.ASIGNADO"/> (sin los pasos intermedios del alta por Excel, porque la
    /// unidad ya viene elegida a mano).
    /// </summary>
    private async Task<(ResultadoImportacionDto Resultado, ServicioDto Servicio)> ObtenerOCrearRutaAsignadaAsync(
        int empresaId, DateOnly fecha, TimeOnly hora, TipoServicio tipo, int sedeId, int unidadOperativaId, string mensajeSiRutaProtegida)
    {
        var jornadas = await _jornadaServicio.ObtenerPorEmpresaAsync(empresaId);
        var jornada = jornadas.FirstOrDefault(j => j.FechaOperativa == fecha)
            ?? await _jornadaServicio.CrearAsync(empresaId, new CrearJornadaDto { FechaOperativa = fecha });

        var serviciosDeLaJornada = await _servicioServicio.ObtenerPorJornadaAsync(empresaId, jornada.JornadaId);
        var servicio = serviciosDeLaJornada.FirstOrDefault(s =>
            s.SedeId == sedeId && s.Tipo == tipo && s.Fecha == fecha && s.HoraProgramada == hora && s.UnidadOperativaId == unidadOperativaId);

        var resultado = new ResultadoImportacionDto { JornadaId = jornada.JornadaId };

        if (servicio is null)
        {
            servicio = await _servicioServicio.CrearAsync(empresaId, jornada.JornadaId, new CrearServicioDto
            {
                SedeId = sedeId,
                Fecha = fecha,
                HoraProgramada = hora,
                Tipo = tipo,
                UnidadOperativaId = unidadOperativaId
            });
            await _servicioServicio.CambiarEstadoAsync(empresaId, servicio.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
            await _servicioServicio.CambiarEstadoAsync(empresaId, servicio.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.ASIGNADO });
            servicio = await _servicioServicio.ObtenerPorIdAsync(empresaId, servicio.ServicioId);
            resultado.ServiciosCreados = 1;
        }
        else if (servicio.Estado is EstadoServicio.CANCELADO or EstadoServicio.FINALIZADO or EstadoServicio.EN_CURSO)
        {
            throw new InvalidOperationException(mensajeSiRutaProtegida);
        }

        resultado.ServicioId = servicio!.ServicioId;
        return (resultado, servicio);
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
        var empleado = await AsegurarEmpleadoAsync(empresaId, fila, resultado);

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

    private sealed record UnidadDisponible(int UnidadOperativaId, int Capacidad);

    private sealed record Pendiente(FilaHoja Fila, ProgramacionDto Programacion);

    /// <summary>Identifica una ruta real (jornada + sede + tipo + fecha + hora), sin importar en qué bloque del Excel venía cada fila.</summary>
    private sealed record ClaveRuta(DateOnly ClaveJornada, DateOnly FechaServicio, int SedeId, TipoServicio Tipo, TimeOnly Hora);

    private sealed record RutaPendiente(Sede Sede, string Titulo, List<Pendiente> Pendientes);

    private async Task EjecutarAsync(
        int empresaId, Analisis analisis, DateOnly fechaOperativa, int? unidadOperativaId, bool repartirEntreUnidades, int? sedeGeneralId, ResultadoImportacionDto resultado)
    {
        // Una jornada por fecha operativa: si la hoja trae la fecha de cada fila, cada día va a su propia jornada.
        var jornadasExistentes = await _jornadaServicio.ObtenerPorEmpresaAsync(empresaId);
        var jornadasCargadas = new Dictionary<DateOnly, (JornadaDto Jornada, List<ServicioDto> Servicios)>();
        var programaciones = await _programacionServicio.ObtenerPorEmpresaAsync(empresaId);
        var sedes = (await _sedeRepositorio.ObtenerPorEmpresaAsync(empresaId)).Where(s => s.Activa).ToList();
        var unidades = repartirEntreUnidades ? await ObtenerUnidadesDisponiblesAsync(empresaId) : new List<UnidadDisponible>();
        var mapaZonas = repartirEntreUnidades ? await ObtenerZonaPorBarrioAsync(empresaId) : MapaZonas.Vacio;
        // Rutas que ya ocupan a cada unidad (existentes y las que va creando esta importación), para no darle
        // a un conductor dos rutas que se crucen (ver ReglasUnidadOperativa.SeCruzan: a la misma hora solo
        // puede hacer una entrada y una salida de la misma sede).
        var ocupaciones = new List<(int UnidadOperativaId, DateOnly Fecha, TimeOnly Hora, TipoServicio Tipo, int SedeId)>();
        var serviciosNuevos = new List<ServicioDto>();

        async Task<(JornadaDto Jornada, List<ServicioDto> Servicios)> ObtenerJornadaAsync(DateOnly fechaJornada)
        {
            if (jornadasCargadas.TryGetValue(fechaJornada, out var cargada))
            {
                return cargada;
            }

            var jornadaDelDia = jornadasExistentes.FirstOrDefault(j => j.FechaOperativa == fechaJornada)
                ?? await _jornadaServicio.CrearAsync(empresaId, new CrearJornadaDto { FechaOperativa = fechaJornada });
            var existentes = await _servicioServicio.ObtenerPorJornadaAsync(empresaId, jornadaDelDia.JornadaId);
            // Un servicio finalizado o cancelado ya no ocupa a su conductor: su unidad puede volver a
            // repartirse en una hora que, en la vida real, ya dejó de estar en curso.
            foreach (var servicioExistente in existentes.Where(s => s.UnidadOperativaId is not null && s.Estado is not (EstadoServicio.FINALIZADO or EstadoServicio.CANCELADO)))
            {
                ocupaciones.Add((servicioExistente.UnidadOperativaId!.Value, servicioExistente.Fecha, servicioExistente.HoraProgramada, servicioExistente.Tipo, servicioExistente.SedeId));
            }

            // Servicios de importaciones anteriores que quedaron en Borrador (por ejemplo, tras un fallo) también avanzan.
            serviciosNuevos.AddRange(existentes.Where(s => s.Estado == EstadoServicio.BORRADOR));

            if (jornadasCargadas.Count == 0)
            {
                resultado.JornadaId = jornadaDelDia.JornadaId;
            }

            return jornadasCargadas[fechaJornada] = (jornadaDelDia, existentes);
        }

        try
        {
            // Primera pasada: arma los pendientes de cada bloque del Excel y los agrupa por ruta real
            // (jornada + sede + tipo + fecha + hora). Así, si la hoja trae la misma ruta partida en varios
            // bloques (por ejemplo, filas de otro horario intercaladas, o una fecha en unas filas y no en
            // otras que igual cae en el mismo día), todos sus pasajeros se reparten juntos en vez de abrir
            // una ruta nueva y agotar unidades por cada bloque.
            var rutasPendientes = new Dictionary<ClaveRuta, RutaPendiente>();
            var programacionesYaPendientes = new HashSet<int>();

            foreach (var grupo in analisis.Hoja.Grupos)
            {
                var sede = await ObtenerSedeAsync(empresaId, grupo.NombreSede, sedeGeneralId, sedes, resultado);
                var fechaServicio = grupo.Fecha ?? (analisis.Vista.CruzaMedianoche && grupo.Hora < LimiteMadrugada ? fechaOperativa.AddDays(1) : fechaOperativa);
                var claveJornada = grupo.Fecha ?? fechaOperativa;
                await ObtenerJornadaAsync(claveJornada);

                var pendientesDelGrupo = new List<Pendiente>();
                foreach (var fila in grupo.Filas)
                {
                    var empleado = await AsegurarEmpleadoAsync(empresaId, fila, resultado);

                    var programacion = programaciones.FirstOrDefault(p =>
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
                        programaciones.Add(programacion);
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

            // Segunda pasada: por cada ruta real ya unificada, reparte entre unidades (o usa la indicada) y crea el servicio.
            foreach (var (clave, ruta) in rutasPendientes)
            {
                var (jornada, servicios) = await ObtenerJornadaAsync(clave.ClaveJornada);
                var pendientes = ruta.Pendientes;

                var destinos = new List<(int? Unidad, List<Pendiente> Filas, ServicioDto? ServicioExistente)>();
                if (repartirEntreUnidades)
                {
                    var usadas = ocupaciones
                        .Where(o => ReglasUnidadOperativa.SeCruzan(o.Fecha, o.Hora, o.Tipo, o.SedeId, clave.FechaServicio, clave.Hora, clave.Tipo, clave.SedeId))
                        .Select(o => o.UnidadOperativaId)
                        .ToHashSet();
                    var usadasAntes = new HashSet<int>(usadas);

                    // Si una importación anterior ya dejó servicios para esta misma ruta con cupo libre, se
                    // completan primero (ver AGENTS.md §27: varias importaciones complementan la programación),
                    // pero solo con pendientes de la MISMA zona que ya traían: nunca se mezclan zonas distintas
                    // en un mismo vehículo solo porque coincidan en sede/hora/tipo entre importaciones separadas.
                    var candidatos = servicios.Where(s =>
                        s.SedeId == clave.SedeId && s.Tipo == clave.Tipo && s.Fecha == clave.FechaServicio && s.HoraProgramada == clave.Hora
                        && s.UnidadOperativaId is not null && s.Estado is not (EstadoServicio.FINALIZADO or EstadoServicio.CANCELADO)).ToList();

                    var zonaPorCandidato = new Dictionary<int, string>();
                    foreach (var candidato in candidatos)
                    {
                        zonaPorCandidato[candidato.ServicioId] = await ResolverZonaDelServicioAsync(candidato, mapaZonas);
                    }

                    // "usadas" ya trae, de antemano, las unidades con algún servicio activo a esta hora (ver
                    // ObtenerJornadaAsync): no sirve para filtrar candidatos a completar, así que se lleva un
                    // control aparte solo para no completar el mismo servicio existente dos veces en este import.
                    var candidatosCompletados = new HashSet<int>();
                    var pendientesRestantes = new List<Pendiente>(pendientes.Count);
                    foreach (var grupoZona in pendientes.GroupBy(p => ResolverZona(p.Fila.Barrio, p.Fila.Direccion, mapaZonas)))
                    {
                        var pendientesDeLaZona = grupoZona.ToList();

                        // Completa TODOS los servicios existentes de esta misma zona/corredor que todavía tengan
                        // cupo, no solo el primero: si el primero ya está lleno, no se abre una ruta nueva
                        // mientras otro servicio del mismo corredor tenga espacio libre.
                        foreach (var candidato in candidatos.Where(c =>
                            !candidatosCompletados.Contains(c.ServicioId) && zonaPorCandidato[c.ServicioId] == grupoZona.Key))
                        {
                            if (pendientesDeLaZona.Count == 0)
                            {
                                break;
                            }

                            var unidadDelCandidato = unidades.FirstOrDefault(u => u.UnidadOperativaId == candidato.UnidadOperativaId!.Value);
                            if (unidadDelCandidato is null)
                            {
                                continue;
                            }

                            var yaAsignados = (await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(candidato.ServicioId)).Count;
                            var cupoLibre = unidadDelCandidato.Capacidad - yaAsignados;
                            if (cupoLibre <= 0)
                            {
                                continue;
                            }

                            var aCompletar = pendientesDeLaZona.Take(cupoLibre).ToList();
                            destinos.Add((candidato.UnidadOperativaId, aCompletar, candidato));
                            pendientesDeLaZona = pendientesDeLaZona.Skip(cupoLibre).ToList();
                            candidatosCompletados.Add(candidato.ServicioId);
                            usadas.Add(candidato.UnidadOperativaId!.Value);
                        }

                        pendientesRestantes.AddRange(pendientesDeLaZona);
                    }

                    pendientes = pendientesRestantes;

                    if (pendientes.Count > 0)
                    {
                        foreach (var (unidad, filas) in Repartir(unidades, usadas, pendientes, mapaZonas, ruta.Titulo, clave.Hora, clave.FechaServicio, resultado.Advertencias))
                        {
                            destinos.Add((unidad, filas, null));
                        }
                    }

                    foreach (var unidadNueva in usadas.Except(usadasAntes))
                    {
                        ocupaciones.Add((unidadNueva, clave.FechaServicio, clave.Hora, clave.Tipo, clave.SedeId));
                    }
                }
                else
                {
                    destinos.Add((unidadOperativaId, pendientes, null));
                }

                foreach (var (unidad, filas, servicioExistente) in destinos)
                {
                    var servicio = servicioExistente ?? (repartirEntreUnidades
                        ? null
                        : servicios.FirstOrDefault(s =>
                            s.SedeId == clave.SedeId && s.Tipo == clave.Tipo && s.Fecha == clave.FechaServicio && s.HoraProgramada == clave.Hora
                            && s.Estado is not (EstadoServicio.FINALIZADO or EstadoServicio.CANCELADO)));

                    if (servicio is null)
                    {
                        servicio = await _servicioServicio.CrearAsync(empresaId, jornada.JornadaId, new CrearServicioDto
                        {
                            SedeId = clave.SedeId,
                            Fecha = clave.FechaServicio,
                            HoraProgramada = clave.Hora,
                            Tipo = clave.Tipo,
                            UnidadOperativaId = unidad
                        });
                        servicios.Add(servicio);
                        serviciosNuevos.Add(servicio);
                        resultado.ServiciosCreados++;
                    }

                    foreach (var pendiente in filas)
                    {
                        try
                        {
                            await _servicioPasajeroServicio.CrearAsync(
                                empresaId, servicio.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = pendiente.Programacion.ProgramacionTransporteId });
                            resultado.PasajerosAsignados++;
                        }
                        catch (InvalidOperationException)
                        {
                            // Entre que se armó la lista de pendientes (primera pasada) y este punto, otra
                            // importación o acción concurrente sobre la misma fecha pudo haber asignado esta
                            // misma programación a un servicio: si es así, no es un error real, solo se
                            // descarta esta fila en vez de abortar toda la importación con Estado=ERROR.
                            if (await _servicioPasajeroRepositorio.ObtenerPorProgramacionAsync(pendiente.Programacion.ProgramacionTransporteId) is null)
                            {
                                throw;
                            }

                            resultado.FilasOmitidas++;
                        }
                    }
                }
            }
        }
        finally
        {
            // Aunque la importación falle a mitad (por ejemplo, por falta de unidades), lo ya creado avanza de estado y no queda en Borrador.
            await AvanzarEstadoDeServiciosAsync(empresaId, serviciosNuevos);
        }
    }

    private async Task AvanzarEstadoDeServiciosAsync(int empresaId, List<ServicioDto> serviciosNuevos)
    {
        foreach (var servicioNuevo in serviciosNuevos)
        {
            await _servicioServicio.CambiarEstadoAsync(empresaId, servicioNuevo.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
            if (servicioNuevo.UnidadOperativaId is not null)
            {
                await _servicioServicio.CambiarEstadoAsync(empresaId, servicioNuevo.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.ASIGNADO });
            }
        }
    }

    private const string SinZonaAsignada = "Sin zona asignada";

    /// <summary>
    /// Marca un servicio existente cuyos pasajeros ya mezclan más de una zona (típicamente porque se
    /// armó antes de que existieran las Zonas, o antes de que este chequeo existiera). Nunca coincide
    /// con ninguna zona real ni con <see cref="SinZonaAsignada"/>, así que ese servicio deja de
    /// completarse automáticamente y las importaciones futuras arman una ruta nueva en su lugar.
    /// </summary>
    private const string ZonaMezclada = "__zona_mezclada__";

    /// <summary>Resuelve el grupo de reparto (zona o corredor) de una persona, o <see cref="SinZonaAsignada"/> si su barrio no está clasificado.</summary>
    private static string ResolverZona(string barrio, string? direccion, MapaZonas mapa) =>
        ResolverZonaCandidata(barrio, direccion, mapa)?.Grupo ?? SinZonaAsignada;

    /// <summary>
    /// Elige, entre las zonas cuyo barrio coincide con el de la persona, la que le corresponde. Un mismo
    /// nombre de barrio puede existir en Villamaría y en Manizales (por ejemplo, "Nuevo Horizonte"): si la
    /// dirección pide un cruce de calle y carrera que no existe en Villamaría (ver
    /// <see cref="ReglasNomenclaturaVillamaria"/>), se descartan las zonas de la macrozona Villamaría; si
    /// la dirección sí es posible en Villamaría, se prefieren esas. Sin dirección reconocible se decide
    /// solo por el nombre del barrio, como siempre. Devuelve <c>null</c> si ninguna zona le sirve.
    /// </summary>
    private static ZonaCandidata? ResolverZonaCandidata(string barrio, string? direccion, MapaZonas mapa)
    {
        if (!mapa.ZonasPorBarrio.TryGetValue(Normalizar(barrio), out var candidatas))
        {
            return null;
        }

        return ReglasNomenclaturaVillamaria.DireccionPuedeSerDeVillamaria(direccion) switch
        {
            false => candidatas.LastOrDefault(c => !c.EsVillamaria),
            true => candidatas.LastOrDefault(c => c.EsVillamaria) ?? candidatas[^1],
            null => candidatas[^1],
        };
    }

    /// <summary>
    /// Resuelve la zona a la que pertenece un servicio ya existente, mirando el barrio actual de cada
    /// uno de sus pasajeros: solo se considera "de una zona" si todos coinciden en la misma (si no,
    /// devuelve <see cref="ZonaMezclada"/>). Se usa exclusivamente para decidir si una importación
    /// nueva puede completar ese servicio sin mezclar zonas distintas en el mismo vehículo.
    /// </summary>
    private async Task<string> ResolverZonaDelServicioAsync(ServicioDto servicio, MapaZonas mapaZonas)
    {
        var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicio.ServicioId);
        if (pasajeros.Count == 0)
        {
            return SinZonaAsignada;
        }

        string? zonaComun = null;
        foreach (var pasajero in pasajeros)
        {
            var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(pasajero.EmpleadoId);
            var zona = empleado is null ? SinZonaAsignada : ResolverZona(empleado.Barrio, pasajero.DireccionRecogida, mapaZonas);
            if (zonaComun is null)
            {
                zonaComun = zona;
            }
            else if (zonaComun != zona)
            {
                return ZonaMezclada;
            }
        }

        return zonaComun!;
    }

    /// <summary>Mínimo de pasajeros que debe llevar una ruta nueva antes de abrirla, para no enviar un carro por 1-2-3 personas cuando otra zona vecina puede compartirlo.</summary>
    private const int MinimoPasajerosPorRuta = 4;

    /// <summary>
    /// Bolsa de pendientes que se reparte junta en una misma ruta: empieza
    /// siendo una zona/corredor, pero puede ser el resultado de fusionar
    /// varias zonas pequeñas y cercanas (ver <see cref="FusionarZonasPequenas"/>).
    /// </summary>
    private sealed class BolsaZona
    {
        public BolsaZona(string clave, List<Pendiente> pendientes, HashSet<string> macroZonas)
        {
            Claves.Add(clave);
            Pendientes.AddRange(pendientes);
            MacroZonas = macroZonas;
        }

        public List<string> Claves { get; } = new();
        public List<Pendiente> Pendientes { get; } = new();

        /// <summary>
        /// Macrozonas de TODOS los barrios de la bolsa (no solo la primera): una bolsa ya fusionada
        /// por un corredor vial puede abarcar zonas de macrozonas distintas (un corredor real puede
        /// cruzar el límite administrativo de una comuna), así que dos bolsas se consideran cercanas
        /// si comparten AL MENOS una macrozona, no solo si coincide "la" macrozona de cada una.
        /// </summary>
        public HashSet<string> MacroZonas { get; }

        public bool EsSinZona => Claves.Contains(SinZonaAsignada);
        public string Etiqueta => string.Join(" + ", Claves);
    }

    /// <summary>
    /// Decide qué pendientes lleva cada unidad para una ruta (misma sede,
    /// tipo, fecha y hora). Primero agrupa a los pendientes por zona (según
    /// el barrio de cada uno): una misma ruta puede necesitar varios
    /// conductores en paralelo, uno por zona, porque un solo carro no
    /// alcanza a cubrir toda la ciudad en el tiempo del servicio (ver
    /// AGENTS.md sobre continuidad geográfica; nunca se codifican barrios
    /// fijos, se usa la lista de zonas que el propio coordinador definió).
    /// Antes de asignar unidades, fusiona zonas con menos de
    /// <see cref="MinimoPasajerosPorRuta"/> pendientes con otra zona de la
    /// misma macrozona (ver <see cref="FusionarZonasPequenas"/>): no vale la
    /// pena enviar un carro por 1-2-3 personas si hay otra zona vecina que
    /// puede compartir el vehículo.
    /// Dentro de cada bolsa ya fusionada, usa las unidades libres a esa hora,
    /// las de mayor capacidad primero y solo las necesarias, repartiendo de
    /// forma pareja sin superar la capacidad de ninguna. Si a alguna bolsa no
    /// le alcanzan las unidades libres, no bloquea la importación: dejando
    /// esos pendientes agrupados en una última entrada con unidad nula
    /// (servicio sin conductor asignado), para que el coordinador los
    /// reasigne a mano.
    /// </summary>
    private static List<(int? Unidad, List<Pendiente> Filas)> Repartir(
        List<UnidadDisponible> unidades, HashSet<int> usadas, List<Pendiente> pendientes,
        MapaZonas mapaZonas, string titulo, TimeOnly hora, DateOnly fecha, List<string> advertencias)
    {
        var gruposPorZona = pendientes.GroupBy(p => ResolverZona(p.Fila.Barrio, p.Fila.Direccion, mapaZonas)).ToList();

        var barriosSinZona = gruposPorZona.FirstOrDefault(g => g.Key == SinZonaAsignada)?.Select(p => p.Fila.Barrio).Distinct().ToList();
        if (barriosSinZona is { Count: > 0 })
        {
            advertencias.Add(
                $"Estos barrios de {titulo} ({hora:HH\\:mm}) no están asignados a ninguna zona, así que se reparten aparte: {string.Join(", ", barriosSinZona)}.");
        }

        var bolsas = FusionarZonasPequenas(gruposPorZona, mapaZonas, advertencias, titulo, hora);

        var resultado = new List<(int?, List<Pendiente>)>();
        var sinAsignar = new List<Pendiente>();
        foreach (var bolsa in bolsas.OrderByDescending(b => b.Pendientes.Count))
        {
            var pendientesDeLaZona = bolsa.Pendientes;
            var libres = unidades.Where(u => !usadas.Contains(u.UnidadOperativaId)).OrderByDescending(u => u.Capacidad).ToList();

            var elegidas = new List<UnidadDisponible>();
            var capacidad = 0;
            foreach (var unidad in libres)
            {
                if (capacidad >= pendientesDeLaZona.Count)
                {
                    break;
                }

                elegidas.Add(unidad);
                capacidad += unidad.Capacidad;
            }

            var aAsignar = pendientesDeLaZona;
            if (capacidad < pendientesDeLaZona.Count)
            {
                var nombreZona = bolsa.EsSinZona ? "sin zona asignada" : $"la zona \"{bolsa.Etiqueta}\"";
                var faltan = pendientesDeLaZona.Count - capacidad;
                advertencias.Add(
                    $"No hay unidades suficientes para {faltan} pasajero(s) de {nombreZona} en {titulo} ({hora:HH\\:mm}, {fecha:yyyy-MM-dd}): "
                    + $"la capacidad libre a esa hora es {capacidad}. Quedan sin conductor asignado al final de la lista de este horario para reasignarlos manualmente.");
                aAsignar = pendientesDeLaZona.Take(capacidad).ToList();
                sinAsignar.AddRange(pendientesDeLaZona.Skip(capacidad));
            }

            var restantes = aAsignar.Count;
            var porAsignar = elegidas.OrderBy(u => u.Capacidad).ToList();
            var indice = 0;
            for (var i = 0; i < porAsignar.Count; i++)
            {
                var cantidad = Math.Min(porAsignar[i].Capacidad, (int)Math.Ceiling(restantes / (double)(porAsignar.Count - i)));
                if (cantidad <= 0)
                {
                    continue;
                }

                resultado.Add((porAsignar[i].UnidadOperativaId, aAsignar.Skip(indice).Take(cantidad).ToList()));
                usadas.Add(porAsignar[i].UnidadOperativaId);
                indice += cantidad;
                restantes -= cantidad;
            }
        }

        if (sinAsignar.Count > 0)
        {
            resultado.Add((null, sinAsignar));
        }

        return resultado;
    }

    /// <summary>
    /// Fusiona zonas con menos de <see cref="MinimoPasajerosPorRuta"/>
    /// pendientes con otra zona de la MISMA macrozona (comuna/sector amplio
    /// que el coordinador ya declaró), hasta alcanzar el mínimo o hasta
    /// quedarse sin otra zona vecina para juntar. La cercanía nunca se
    /// inventa por coordenadas o distancia calculada (no hay ese dato): se
    /// usa únicamente la macrozona que el propio coordinador asignó a cada
    /// zona, así que dos zonas sin macrozona (o de macrozonas distintas)
    /// nunca se fusionan entre sí. El balde "sin zona asignada" tampoco se
    /// fusiona nunca: no hay ningún dato de cercanía para decidir con qué
    /// otra zona juntarlo.
    /// </summary>
    private static List<BolsaZona> FusionarZonasPequenas(
        List<IGrouping<string, Pendiente>> gruposPorZona, MapaZonas mapaZonas,
        List<string> advertencias, string titulo, TimeOnly hora)
    {
        var bolsas = gruposPorZona
            .Where(g => g.Key != SinZonaAsignada)
            .Select(g => new BolsaZona(g.Key, g.ToList(), ResolverMacroZonas(g, mapaZonas)))
            .ToList();

        var sinFusionPosible = new HashSet<BolsaZona>();
        while (true)
        {
            var pequena = bolsas.FirstOrDefault(b => b.Pendientes.Count < MinimoPasajerosPorRuta && !sinFusionPosible.Contains(b));
            if (pequena is null)
            {
                break;
            }

            // Solo se fusiona con otra bolsa que TODAVÍA esté por debajo del mínimo: una bolsa que ya
            // alcanzó el mínimo deja de ser candidata, para que no seguir absorbiendo zonas indefinidamente
            // (dos zonas pueden compartir macrozona con una tercera sin ser cercanas entre sí; sin este
            // límite, una bolsa ya fusionada podía seguir creciendo y terminar mezclando comunas sin
            // relación real entre sí con tal de que cada fusión, vista de a una, compartiera una macrozona).
            var vecina = bolsas
                .Where(b => b != pequena && b.Pendientes.Count < MinimoPasajerosPorRuta && b.MacroZonas.Overlaps(pequena.MacroZonas))
                .OrderBy(b => b.Pendientes.Count)
                .FirstOrDefault();

            if (vecina is null)
            {
                sinFusionPosible.Add(pequena);
                advertencias.Add(
                    $"La zona \"{pequena.Etiqueta}\" de {titulo} ({hora:HH\\:mm}) tiene menos de {MinimoPasajerosPorRuta} pasajeros y no hay otra zona de la misma "
                    + "macrozona para juntarla en una sola ruta: se reparte igual, sola.");
                continue;
            }

            vecina.Claves.AddRange(pequena.Claves);
            vecina.Pendientes.AddRange(pequena.Pendientes);
            vecina.MacroZonas.UnionWith(pequena.MacroZonas);
            bolsas.Remove(pequena);
            sinFusionPosible.Remove(vecina);
        }

        var sinZona = gruposPorZona.FirstOrDefault(g => g.Key == SinZonaAsignada);
        if (sinZona is not null)
        {
            bolsas.Add(new BolsaZona(sinZona.Key, sinZona.ToList(), new HashSet<string>()));
        }

        return bolsas;
    }

    /// <summary>Macrozonas de todos los barrios de una bolsa (una bolsa ya fusionada por corredor vial puede tener barrios de más de una macrozona).</summary>
    private static HashSet<string> ResolverMacroZonas(IEnumerable<Pendiente> pendientes, MapaZonas mapaZonas)
    {
        var resultado = new HashSet<string>();
        foreach (var pendiente in pendientes)
        {
            if (ResolverZonaCandidata(pendiente.Fila.Barrio, pendiente.Fila.Direccion, mapaZonas)?.MacroZona is { } macroZona)
            {
                resultado.Add(macroZona);
            }
        }

        return resultado;
    }

    /// <summary>
    /// Barrio (normalizado) → zonas que lo contienen, ver <see cref="ObtenerZonaPorBarrioAsync"/>. Un barrio
    /// puede estar en más de una zona cuando el mismo nombre existe en dos ciudades; la elección la hace
    /// <see cref="ResolverZonaCandidata"/> mirando la dirección de la persona.
    /// </summary>
    private sealed record MapaZonas(Dictionary<string, List<ZonaCandidata>> ZonasPorBarrio)
    {
        public static MapaZonas Vacio { get; } = new(new Dictionary<string, List<ZonaCandidata>>());
    }

    /// <summary>Una zona posible para un barrio: su grupo de reparto (zona o corredor), su macrozona y si es de Villamaría.</summary>
    private sealed record ZonaCandidata(string Grupo, string? MacroZona, bool EsVillamaria);

    /// <summary>
    /// Arma, a partir de las zonas activas que el coordinador definió para la
    /// empresa, el mapa barrio (normalizado) → "grupo de reparto" y el mapa
    /// barrio → macrozona. El grupo es normalmente el nombre de la zona, pero
    /// si la zona pertenece a un <see cref="CorredorVial"/> activo, el grupo
    /// es el nombre de ESE corredor: así, varias zonas sobre el mismo camino
    /// real (por ejemplo, para llegar a Morrogacho hay que pasar por La
    /// Francia) se tratan como una sola bolsa de pasajeros antes de repartir
    /// por capacidad, en vez de exigir un conductor por zona. La macrozona
    /// (comuna o sector amplio, ver <see cref="MacroZona"/>) se usa aparte,
    /// solo para decidir qué zonas pequeñas están lo bastante cerca como para
    /// fusionarse y no abrir una ruta de 1-2-3 pasajeros (ver
    /// <see cref="FusionarZonasPequenas"/>). Un barrio que no aparece en
    /// ninguna zona simplemente no tiene entrada en ninguno de los dos mapas.
    /// </summary>
    private async Task<MapaZonas> ObtenerZonaPorBarrioAsync(int empresaId)
    {
        var zonas = (await _zonaRepositorio.ObtenerPorEmpresaAsync(empresaId)).Where(z => z.Activa);
        var zonasPorBarrio = new Dictionary<string, List<ZonaCandidata>>();
        foreach (var zona in zonas)
        {
            var grupo = zona.CorredorVial is { Activo: true } corredor ? corredor.Nombre : zona.Nombre;
            var macroZona = zona.MacroZona is { Activa: true } macro ? macro.Nombre : null;
            // Las zonas de la macrozona "Villamaría" son las de esa ciudad (mismo criterio que usa el frontend).
            var esVillamaria = zona.MacroZona is not null && Normalizar(zona.MacroZona.Nombre) == "VILLAMARIA";
            foreach (var barrio in zona.Barrios)
            {
                var clave = Normalizar(barrio);
                if (!zonasPorBarrio.TryGetValue(clave, out var candidatas))
                {
                    zonasPorBarrio[clave] = candidatas = new List<ZonaCandidata>();
                }

                candidatas.Add(new ZonaCandidata(grupo, macroZona, esVillamaria));
            }
        }

        return new MapaZonas(zonasPorBarrio);
    }

    /// <summary>
    /// De los barrios indicados, los que no coinciden con el de ninguna zona activa de la empresa (texto
    /// exacto, sin mayúsculas ni tildes), en orden alfabético y sin repetidos. Se calcula siempre al
    /// validar o importar, aunque el coordinador no haya elegido repartir entre unidades esta vez: sirve
    /// para que note y corrija estos barrios antes de que el reparto automático los deje sin zona.
    /// </summary>
    private async Task<List<string>> ObtenerBarriosSinZonaAsync(int empresaId, IEnumerable<(string Barrio, string Direccion)> personas)
    {
        var mapaZonas = await ObtenerZonaPorBarrioAsync(empresaId);
        return personas
            .Where(p => !string.IsNullOrWhiteSpace(p.Barrio))
            .Where(p => ResolverZonaCandidata(p.Barrio, p.Direccion, mapaZonas) is null)
            .Select(p => p.Barrio.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(b => b, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<List<UnidadDisponible>> ObtenerUnidadesDisponiblesAsync(int empresaId)
    {
        var resultado = new List<UnidadDisponible>();
        var conductores = await _conductorRepositorio.ObtenerPorEmpresaAsync(empresaId);
        foreach (var conductor in conductores.Where(c => c.Activo && c.VinculacionesConductorEmpresa.Any(v => v.EmpresaId == empresaId && v.Activa)))
        {
            foreach (var unidad in (await _unidadRepositorio.ObtenerPorConductorAsync(conductor.ConductorId)).Where(u => u.Activa))
            {
                var vehiculo = await _vehiculoRepositorio.ObtenerPorIdAsync(unidad.VehiculoId);
                if (vehiculo is { Activo: true } && vehiculo.Capacidad > 0)
                {
                    resultado.Add(new UnidadDisponible(unidad.UnidadOperativaId, vehiculo.Capacidad));
                }
            }
        }

        return resultado;
    }

    /// <summary>
    /// Devuelve la sede del grupo: la que coincide con el nombre de la hoja;
    /// si el nombre no existe todavía, la crea (las sedes vienen del Excel,
    /// no se digitan aparte). Si la hoja no trae sede (columna ausente o
    /// celda vacía), usa la sede general que eligió el coordinador al
    /// importar; si tampoco la indicó, falla.
    /// </summary>
    private async Task<Sede> ObtenerSedeAsync(int empresaId, string nombreEnHoja, int? sedeGeneralId, List<Sede> sedes, ResultadoImportacionDto resultado)
    {
        var existente = BuscarSede(sedes, nombreEnHoja);
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

    /// <summary>
    /// Devuelve el empleado de la cédula dentro de la empresa, creándolo o
    /// vinculándolo según el caso: cuenta inexistente (se crea una cuenta
    /// pendiente de que la persona se registre), cuenta ya registrada sin
    /// perfil de empleado (se vincula) o empleado de otra empresa (cambio
    /// automático de empresa; ver <c>AGENTS.md</c> §15).
    /// </summary>
    private async Task<Empleado> AsegurarEmpleadoAsync(int empresaId, FilaHoja fila, ResultadoImportacionDto resultado)
    {
        var usuario = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(fila.Cedula);
        var usuarioEsNuevo = usuario is null;

        if (usuario is null)
        {
            usuario = new Usuario
            {
                Cedula = fila.Cedula,
                NombreCompleto = fila.NombreCompleto,
                Telefono = fila.Celular,
                PasswordHash = null,
                CorreoConfirmado = false,
                Activo = true
            };
            await _usuarioRepositorio.AgregarAsync(usuario);
            try
            {
                await _usuarioRepositorio.GuardarCambiosAsync();
            }
            catch (Exception)
            {
                // Otra importación concurrente sobre la misma cédula (por ejemplo, un reintento por doble
                // clic mientras la primera todavía estaba procesando) pudo haber creado este mismo usuario
                // entre la consulta de arriba y este guardado: si ya existe, se usa ese en vez de abortar
                // toda la importación por un choque de llave única que en realidad no es un error de datos.
                // Se descarta primero el usuario fallido: si se dejara marcado como pendiente de agregar,
                // el próximo GuardarCambiosAsync lo reintentaría y fallaría siempre con el mismo error.
                _usuarioRepositorio.Descartar(usuario);
                var usuarioConcurrente = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(fila.Cedula);
                if (usuarioConcurrente is null)
                {
                    throw;
                }

                usuario = usuarioConcurrente;
                usuarioEsNuevo = false;
            }
        }

        var empleado = await _empleadoRepositorio.ObtenerPorUsuarioIdAsync(usuario.UsuarioId);

        if (empleado is not null && empleado.EmpresaId == empresaId)
        {
            return empleado;
        }

        if (empleado is null)
        {
            empleado = new Empleado
            {
                UsuarioId = usuario.UsuarioId,
                EmpresaId = empresaId,
                NombreCompleto = fila.NombreCompleto,
                Telefono = fila.Celular,
                Direccion = fila.Direccion,
                Barrio = fila.Barrio,
                Activo = true
            };
            await _empleadoRepositorio.AgregarAsync(empleado);
            try
            {
                await _empleadoRepositorio.GuardarCambiosAsync();
            }
            catch (Exception)
            {
                // Mismo caso de carrera que con el usuario, pero para el empleado (único por UsuarioId).
                _empleadoRepositorio.Descartar(empleado);
                var empleadoConcurrente = await _empleadoRepositorio.ObtenerPorUsuarioIdAsync(usuario.UsuarioId);
                if (empleadoConcurrente is null)
                {
                    throw;
                }

                empleado = empleadoConcurrente;
                empleado.EmpresaId = empresaId;
            }

            if (usuarioEsNuevo)
            {
                resultado.EmpleadosCreados++;
            }
            else
            {
                resultado.EmpleadosVinculados++;
            }
        }
        else
        {
            empleado.EmpresaId = empresaId;
            resultado.EmpleadosVinculados++;
        }

        var rolEmpleado = usuario.UsuarioRoles.FirstOrDefault(r => r.Rol == Rol.EMPLEADO);
        if (rolEmpleado is null)
        {
            await _usuarioRolRepositorio.AgregarAsync(new UsuarioRol { UsuarioId = usuario.UsuarioId, Rol = Rol.EMPLEADO, EmpresaId = empresaId, Activo = true });
            await _usuarioRolRepositorio.GuardarCambiosAsync();
        }
        else
        {
            rolEmpleado.EmpresaId = empresaId;
            rolEmpleado.Activo = true;
        }

        await _empleadoRepositorio.GuardarCambiosAsync();
        return empleado;
    }

    private async Task RegistrarAsync(int empresaId, int usuarioCoordinadorId, string nombreArchivo, EstadoImportacionExcel estado)
    {
        await _importacionRepositorio.AgregarAsync(new ImportacionExcel
        {
            EmpresaId = empresaId,
            CoordinadorId = usuarioCoordinadorId,
            NombreArchivo = nombreArchivo,
            FechaImportacion = DateTime.UtcNow,
            Estado = estado
        });
        await _importacionRepositorio.GuardarCambiosAsync();
    }

    private async Task<Analisis> AnalizarAsync(int empresaId, Stream archivo)
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

        var sedes = (await _sedeRepositorio.ObtenerPorEmpresaAsync(empresaId)).Where(s => s.Activa).ToList();
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
            var sede = BuscarSede(sedes, grupo.NombreSede);
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
                    situacion = await ClasificarAsync(empresaId, fila.Cedula);
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

            var claveSede = sede is not null ? $"id:{sede.SedeId}" : $"txt:{Normalizar(grupo.NombreSede)}";
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
        vista.BarriosSinZona = await ObtenerBarriosSinZonaAsync(empresaId, vista.Servicios.SelectMany(s => s.Pasajeros).Select(p => (p.Barrio, p.Direccion)));

        return new Analisis(hoja, vista, sedePorGrupo);
    }

    private async Task<string> ClasificarAsync(int empresaId, string cedula)
    {
        var usuario = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(cedula);
        if (usuario is null)
        {
            return "NUEVO";
        }

        var empleado = await _empleadoRepositorio.ObtenerPorUsuarioIdAsync(usuario.UsuarioId);
        if (empleado is null)
        {
            return "CUENTA_REGISTRADA";
        }

        return empleado.EmpresaId == empresaId ? "EXISTENTE" : "CAMBIA_DE_EMPRESA";
    }

    private static Sede? BuscarSede(List<Sede> sedes, string nombreEnHoja)
    {
        var buscado = Normalizar(nombreEnHoja);
        if (buscado.Length == 0)
        {
            return null;
        }

        return sedes.FirstOrDefault(s => Normalizar(s.Nombre) == buscado)
            ?? sedes.FirstOrDefault(s => Normalizar(s.Nombre).Contains(buscado));
    }

    private static string Normalizar(string texto)
    {
        var descompuesto = texto.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        return new string(descompuesto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }
}
