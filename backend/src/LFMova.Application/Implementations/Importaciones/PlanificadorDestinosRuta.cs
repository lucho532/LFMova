using LFMova.Application.DTOs.Servicios;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Decide, cuando el coordinador pidió repartir entre unidades, a qué servicio va cada pendiente de
/// una ruta: primero completa los servicios que ya existían con cupo libre y luego reparte el resto
/// entre las unidades libres (ver <see cref="RepartidorPorZona"/>). Solo planifica: no crea servicios
/// ni asigna pasajeros, y es una recomendación que el coordinador puede cambiar después.
/// </summary>
public class PlanificadorDestinosRuta
{
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadRepositorio;
    private readonly IVehiculoRepositorio _vehiculoRepositorio;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public PlanificadorDestinosRuta(
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IConductorRepositorio conductorRepositorio,
        IUnidadOperativaRepositorio unidadRepositorio,
        IVehiculoRepositorio vehiculoRepositorio)
    {
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _unidadRepositorio = unidadRepositorio;
        _vehiculoRepositorio = vehiculoRepositorio;
    }

    /// <summary>
    /// Unidades activas de los conductores activos y con vinculación activa a la empresa, cuyo vehículo
    /// está activo y tiene capacidad.
    /// </summary>
    public async Task<List<UnidadDisponible>> ObtenerUnidadesDisponiblesAsync(int empresaId)
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
    /// Reparte los pendientes de la ruta entre los servicios que ya existían para ella y las unidades
    /// libres a esa hora, y deja anotadas en el contexto las unidades que quedan ocupadas.
    /// </summary>
    public async Task<List<DestinoRuta>> PlanificarAsync(ContextoImportacion contexto, ClaveRuta clave, RutaPendiente ruta, List<ServicioDto> servicios)
    {
        var destinos = new List<DestinoRuta>();
        var pendientes = ruta.Pendientes;
        var mapaZonas = contexto.MapaZonas;

        var usadas = contexto.Ocupaciones
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
            && s.UnidadOperativaId is not null && ReglasEstadoServicio.AdmitePasajerosNuevos(s.Estado)).ToList();

        var zonaPorCandidato = new Dictionary<int, string>();
        foreach (var candidato in candidatos)
        {
            zonaPorCandidato[candidato.ServicioId] = await ResolverZonaDelServicioAsync(candidato, mapaZonas);
        }

        // "usadas" ya trae, de antemano, las unidades con algún servicio activo a esta hora (ver
        // CargadorJornadasImportacion.ObtenerAsync): no sirve para filtrar candidatos a completar, así que
        // se lleva un control aparte solo para no completar el mismo servicio existente dos veces en
        // este import.
        var candidatosCompletados = new HashSet<int>();
        var pendientesRestantes = new List<Pendiente>(pendientes.Count);
        foreach (var grupoZona in pendientes.GroupBy(p => mapaZonas.ResolverZona(p.Fila.Barrio, p.Fila.Direccion)))
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

                var unidadDelCandidato = contexto.Unidades.FirstOrDefault(u => u.UnidadOperativaId == candidato.UnidadOperativaId!.Value);
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
                destinos.Add(new DestinoRuta(candidato.UnidadOperativaId, aCompletar, candidato));
                pendientesDeLaZona = pendientesDeLaZona.Skip(cupoLibre).ToList();
                candidatosCompletados.Add(candidato.ServicioId);
                usadas.Add(candidato.UnidadOperativaId!.Value);
            }

            pendientesRestantes.AddRange(pendientesDeLaZona);
        }

        pendientes = pendientesRestantes;

        if (pendientes.Count > 0)
        {
            foreach (var (unidad, filas) in RepartidorPorZona.Repartir(
                contexto.Unidades, usadas, pendientes, mapaZonas, ruta.Titulo, clave.Hora, clave.FechaServicio, contexto.Resultado.Advertencias))
            {
                destinos.Add(new DestinoRuta(unidad, filas, null));
            }
        }

        foreach (var unidadNueva in usadas.Except(usadasAntes))
        {
            contexto.Ocupaciones.Add((unidadNueva, clave.FechaServicio, clave.Hora, clave.Tipo, clave.SedeId));
        }

        return destinos;
    }

    /// <summary>
    /// Resuelve la zona a la que pertenece un servicio ya existente, mirando el barrio actual de cada
    /// uno de sus pasajeros: solo se considera "de una zona" si todos coinciden en la misma (si no,
    /// devuelve <see cref="MapaZonas.ZonaMezclada"/>). Se usa exclusivamente para decidir si una
    /// importación nueva puede completar ese servicio sin mezclar zonas distintas en el mismo vehículo.
    /// </summary>
    private async Task<string> ResolverZonaDelServicioAsync(ServicioDto servicio, MapaZonas mapaZonas)
    {
        var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicio.ServicioId);
        if (pasajeros.Count == 0)
        {
            return MapaZonas.SinZonaAsignada;
        }

        string? zonaComun = null;
        foreach (var pasajero in pasajeros)
        {
            var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(pasajero.EmpleadoId);
            var zona = empleado is null ? MapaZonas.SinZonaAsignada : mapaZonas.ResolverZona(empleado.Barrio, pasajero.DireccionRecogida);
            if (zonaComun is null)
            {
                zonaComun = zona;
            }
            else if (zonaComun != zona)
            {
                return MapaZonas.ZonaMezclada;
            }
        }

        return zonaComun!;
    }
}
