using LFMova.Application.DTOs.Jornadas;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.Jornadas;

/// <summary>
/// Deshace lo que una jornada tiene armado y todavía no es operación real: los servicios sin publicar
/// con sus pasajeros y, si se pide, también las programaciones que quedaron sueltas y la propia
/// jornada. Nunca toca servicios publicados, en curso o finalizados (ver <c>AGENTS.md</c> §17).
/// </summary>
public class DepuradorJornada
{
    private readonly IJornadaRepositorio _jornadaRepositorio;
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IProgramacionTransporteRepositorio _programacionRepositorio;

    /// <summary>Crea el colaborador con sus repositorios.</summary>
    public DepuradorJornada(
        IJornadaRepositorio jornadaRepositorio,
        IServicioRepositorio servicioRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IProgramacionTransporteRepositorio programacionRepositorio)
    {
        _jornadaRepositorio = jornadaRepositorio;
        _servicioRepositorio = servicioRepositorio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _programacionRepositorio = programacionRepositorio;
    }

    /// <summary>Elimina los servicios sin publicar de la jornada y libera a sus pasajeros; las programaciones se conservan.</summary>
    public async Task<DeshacerRepartoDto> DeshacerRepartoAsync(int empresaId, int jornadaId)
    {
        var jornada = await _jornadaRepositorio.ObtenerPorIdAsync(jornadaId);
        if (jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(jornada, empresaId))
        {
            throw new InvalidOperationException("La jornada indicada no existe en esta empresa.");
        }

        var servicios = await _servicioRepositorio.ObtenerPorJornadaAsync(jornadaId);
        var aDeshacer = servicios.Where(s => s.Estado is EstadoServicio.BORRADOR or EstadoServicio.PENDIENTE_ASIGNACION or EstadoServicio.ASIGNADO).ToList();

        var resultado = new DeshacerRepartoDto();
        foreach (var servicio in aDeshacer)
        {
            var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicio.ServicioId);
            foreach (var pasajero in pasajeros)
            {
                await _servicioPasajeroRepositorio.EliminarAsync(pasajero);
                resultado.PasajerosLiberados++;
            }

            await _servicioRepositorio.EliminarAsync(servicio);
            resultado.ServiciosEliminados++;
        }

        // Ambos repositorios comparten el mismo contexto de datos: un solo guardado persiste tanto
        // los pasajeros como los servicios eliminados, en el orden correcto según sus relaciones.
        await _servicioRepositorio.GuardarCambiosAsync();

        return resultado;
    }

    /// <summary>Elimina todo lo importado para la jornada (servicios sin publicar, pasajeros, programaciones sueltas y la jornada si queda vacía); falla si ya hay operación real.</summary>
    public async Task<EliminarRastroDto> EliminarRastroAsync(int empresaId, int jornadaId)
    {
        var jornada = await _jornadaRepositorio.ObtenerPorIdAsync(jornadaId);
        if (jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(jornada, empresaId))
        {
            throw new InvalidOperationException("La jornada indicada no existe en esta empresa.");
        }

        var servicios = await _servicioRepositorio.ObtenerPorJornadaAsync(jornadaId);
        var protegidos = servicios.Where(s => s.Estado is EstadoServicio.PUBLICADO or EstadoServicio.EN_CURSO or EstadoServicio.FINALIZADO).ToList();
        if (protegidos.Count > 0)
        {
            throw new InvalidOperationException(
                "No se puede eliminar: esta jornada ya tiene servicios publicados, en curso o finalizados, que son operación real y no se pueden deshacer.");
        }

        var resultado = new EliminarRastroDto();

        var aDeshacer = servicios.Where(s => s.Estado is EstadoServicio.BORRADOR or EstadoServicio.PENDIENTE_ASIGNACION or EstadoServicio.ASIGNADO).ToList();
        foreach (var servicio in aDeshacer)
        {
            var pasajeros = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicio.ServicioId);
            foreach (var pasajero in pasajeros)
            {
                await _servicioPasajeroRepositorio.EliminarAsync(pasajero);
                resultado.PasajerosEliminados++;
            }

            await _servicioRepositorio.EliminarAsync(servicio);
            resultado.ServiciosEliminados++;
        }

        // Además de los servicios y pasajeros, se eliminan las ProgramacionTransporte de esta fecha que
        // hayan quedado sin ningún ServicioPasajero (por ejemplo, filas de una importación que falló a
        // medias): esas son "rastro" de la importación que impedirían reconocer la cédula como nueva en
        // un reimport desde cero. Se consideran las dos fechas posibles de una importación (ver
        // AgrupadorPendientesRuta.AgruparAsync): la fecha operativa y el día siguiente, para las filas
        // de un horario que cruza medianoche.
        var fechas = new[] { jornada.FechaOperativa, jornada.FechaOperativa.AddDays(1) };
        var programacionesDeLaFecha = (await _programacionRepositorio.ObtenerPorEmpresaAsync(empresaId))
            .Where(p => fechas.Contains(p.Fecha))
            .ToList();
        foreach (var programacion in programacionesDeLaFecha)
        {
            // Si todavía tiene un pasajero de servicio (de un servicio protegido que no se tocó, como uno
            // cancelado con historial), no se elimina: violaría la relación con ese ServicioPasajero.
            if (await _servicioPasajeroRepositorio.ObtenerPorProgramacionAsync(programacion.ProgramacionTransporteId) is not null)
            {
                continue;
            }

            await _programacionRepositorio.EliminarAsync(programacion);
            resultado.ProgramacionesEliminadas++;
        }

        await _servicioRepositorio.GuardarCambiosAsync();

        // Si no quedó ningún servicio (ni siquiera uno cancelado), la jornada también se elimina: así una
        // nueva importación de esa fecha arma una jornada nueva, como si nunca se hubiera importado nada.
        var serviciosRestantes = await _servicioRepositorio.ObtenerPorJornadaAsync(jornadaId);
        if (serviciosRestantes.Count == 0)
        {
            await _jornadaRepositorio.EliminarAsync(jornada);
            await _jornadaRepositorio.GuardarCambiosAsync();
            resultado.JornadaEliminada = true;
        }

        return resultado;
    }
}
