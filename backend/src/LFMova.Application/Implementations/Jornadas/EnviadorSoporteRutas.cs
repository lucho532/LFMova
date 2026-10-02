using LFMova.Application.DTOs.Soportes;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Domain.Entities;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.Jornadas;

/// <summary>
/// Envía por correo a cada conductor un Excel con sus propias rutas
/// publicadas de una jornada, como copia para consultar si la aplicación no
/// está disponible. Cada publicación reenvía el soporte actualizado. Nunca
/// incluye rutas de otros conductores, y un fallo al enviar no deshace la
/// publicación: el conductor igual ve sus rutas en la aplicación.
/// </summary>
public class EnviadorSoporteRutas
{
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly IEmpresaRepositorio _empresaRepositorio;
    private readonly IGeneradorSoporteRutas _generador;
    private readonly IServicioCorreo _servicioCorreo;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public EnviadorSoporteRutas(
        IServicioRepositorio servicioRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IEmpresaRepositorio empresaRepositorio,
        IGeneradorSoporteRutas generador,
        IServicioCorreo servicioCorreo)
    {
        _servicioRepositorio = servicioRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _empresaRepositorio = empresaRepositorio;
        _generador = generador;
        _servicioCorreo = servicioCorreo;
    }

    /// <summary>
    /// Envía el soporte a los conductores de los servicios recién publicados. Cada uno recibe todas sus
    /// rutas de la jornada que ya ve en la aplicación (no solo las de esta publicación), para que el
    /// último correo sea siempre el vigente.
    /// </summary>
    public async Task EnviarAsync(Jornada jornada, IReadOnlyCollection<Servicio> serviciosPublicados)
    {
        if (serviciosPublicados.Count == 0)
        {
            return;
        }

        var visibles = (await _servicioRepositorio.ObtenerPorJornadaAsync(jornada.JornadaId))
            .Where(s => s.UnidadOperativaId is not null && ReglasEstadoServicio.EsVisibleParaConductorYEmpleado(s.Estado))
            .ToList();

        var conductorPorUnidad = new Dictionary<int, int>();
        foreach (var unidadId in visibles.Select(s => s.UnidadOperativaId!.Value).Distinct())
        {
            var unidad = await _unidadOperativaRepositorio.ObtenerPorIdAsync(unidadId);
            if (unidad is not null)
            {
                conductorPorUnidad[unidadId] = unidad.ConductorId;
            }
        }

        var conductoresAfectados = serviciosPublicados
            .Where(s => s.UnidadOperativaId is not null && conductorPorUnidad.ContainsKey(s.UnidadOperativaId.Value))
            .Select(s => conductorPorUnidad[s.UnidadOperativaId!.Value])
            .Distinct()
            .ToList();
        if (conductoresAfectados.Count == 0)
        {
            return;
        }

        var empresa = await _empresaRepositorio.ObtenerPorIdAsync(jornada.EmpresaId);
        foreach (var conductorId in conductoresAfectados)
        {
            var conductor = await _conductorRepositorio.ObtenerPorIdAsync(conductorId);
            var correo = conductor?.Usuario?.Email;
            if (conductor is null || string.IsNullOrWhiteSpace(correo))
            {
                continue;
            }

            var rutas = visibles
                .Where(s => conductorPorUnidad.TryGetValue(s.UnidadOperativaId!.Value, out var dueno) && dueno == conductorId)
                .OrderBy(s => s.Fecha).ThenBy(s => s.HoraProgramada)
                .ToList();

            try
            {
                await EnviarAConductorAsync(conductor, correo, empresa?.Nombre ?? string.Empty, jornada, rutas);
            }
            catch (Exception)
            {
                // El soporte es una copia de respaldo: si el correo falla (proveedor caído, dirección
                // rechazada), la publicación ya está hecha y el conductor ve sus rutas en la aplicación.
                // No se deshace nada ni se deja de enviar a los demás conductores.
            }
        }
    }

    private async Task EnviarAConductorAsync(Conductor conductor, string correo, string nombreEmpresa, Jornada jornada, List<Servicio> rutas)
    {
        var soporte = new SoporteRutasConductor
        {
            NombreConductor = conductor.NombreCompleto,
            NombreEmpresa = nombreEmpresa,
            FechaOperativa = jornada.FechaOperativa
        };

        foreach (var ruta in rutas)
        {
            var rutaSoporte = new RutaSoporte
            {
                Tipo = ruta.Tipo,
                NombreSede = ruta.Sede?.Nombre ?? string.Empty,
                Fecha = ruta.Fecha,
                Hora = ruta.HoraProgramada
            };

            var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(ruta.ServicioId);
            foreach (var pasajero in pasajeros.OrderBy(p => p.Orden))
            {
                var empleado = await _empleadoRepositorio.ObtenerPorIdAsync(pasajero.EmpleadoId);
                rutaSoporte.Pasajeros.Add(new PasajeroSoporte
                {
                    Orden = pasajero.Orden,
                    NombreCompleto = empleado?.NombreCompleto ?? string.Empty,
                    Telefono = empleado?.Telefono ?? string.Empty,
                    // La dirección es la que quedó guardada para ese servicio, no la actual del empleado.
                    Direccion = pasajero.DireccionRecogida,
                    Barrio = empleado?.Barrio ?? string.Empty
                });
            }

            soporte.Rutas.Add(rutaSoporte);
        }

        var fecha = jornada.FechaOperativa;
        var listaRutas = string.Join(string.Empty, rutas.Select(r => $"<li>{FormatoOperacion.DescribirRuta(r)}</li>"));
        await _servicioCorreo.EnviarConAdjuntoAsync(
            correo,
            conductor.NombreCompleto,
            $"Tus rutas del {fecha:dd/MM/yyyy} · {nombreEmpresa}",
            $"<p>Hola {conductor.NombreCompleto},</p>"
                + $"<p>Te enviamos como soporte tus rutas publicadas del <strong>{fecha:dd/MM/yyyy}</strong> en <strong>{nombreEmpresa}</strong>:</p>"
                + $"<ul>{listaRutas}</ul>"
                + "<p>En el archivo adjunto están los pasajeros de cada ruta, en su orden de recogida. Guárdalo: te sirve para consultar "
                + "tus rutas si en algún momento no puedes entrar a LFMova. Si el coordinador cambia algo y vuelve a publicar, "
                + "recibirás un correo nuevo; el más reciente es siempre el vigente.</p>",
            new AdjuntoCorreo($"rutas-{fecha:yyyy-MM-dd}.xlsx", _generador.Generar(soporte)));
    }
}
