using LFMova.Application.DTOs.Servicios;
using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.ServiciosPasajero;

/// <summary>
/// Aplica los cambios que hace el coordinador al reorganizar rutas ya armadas: quitar un pasajero,
/// corregir su dirección de recogida o moverlo a otra ruta del mismo horario, liberando la ruta que
/// quede vacía. No toca rutas en curso o finalizadas ni decide autorización.
/// </summary>
public class ReorganizadorPasajeros
{
    private readonly AccesoServicioPasajero _acceso;
    private readonly AprendizCorredorVial _aprendizCorredor;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IServicioRepositorio _servicioRepositorio;
    private readonly IServicioServicio _servicioServicio;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public ReorganizadorPasajeros(
        AccesoServicioPasajero acceso,
        AprendizCorredorVial aprendizCorredor,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IServicioRepositorio servicioRepositorio,
        IServicioServicio servicioServicio)
    {
        _acceso = acceso;
        _aprendizCorredor = aprendizCorredor;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _servicioRepositorio = servicioRepositorio;
        _servicioServicio = servicioServicio;
    }

    /// <summary>Quita al pasajero de su ruta; si era el último, la ruta también se elimina.</summary>
    public async Task EliminarAsync(int empresaId, int servicioPasajeroId)
    {
        var pasajero = await _acceso.ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        var servicio = await _acceso.ObtenerServicioDeLaEmpresaOFallarAsync(empresaId, pasajero.ServicioId);
        if (servicio.Estado is EstadoServicio.EN_CURSO or EstadoServicio.FINALIZADO)
        {
            throw new InvalidOperationException("No se puede eliminar un pasajero de una ruta en curso o finalizada.");
        }

        await _servicioPasajeroRepositorio.EliminarAsync(pasajero);
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        // Igual que al mover el último pasajero de una ruta a otra (MoverAsync): si esta era la última
        // persona del servicio, ya no representa nada operativo, así que se elimina también.
        var pasajerosRestantes = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicio.ServicioId);
        if (pasajerosRestantes.Count == 0)
        {
            await LiberarServicioVacioAsync(empresaId, servicio);
        }
    }

    /// <summary>Corrige la dirección de recogida del pasajero en ese servicio (no la habitual del empleado).</summary>
    public async Task EditarDireccionAsync(int empresaId, int servicioPasajeroId, EditarDireccionServicioPasajeroDto datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Direccion))
        {
            throw new InvalidOperationException("La dirección no puede quedar vacía.");
        }

        var pasajero = await _acceso.ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        var servicio = await _servicioRepositorio.ObtenerPorIdAsync(pasajero.ServicioId);
        if (servicio is not null && servicio.Estado is EstadoServicio.EN_CURSO or EstadoServicio.FINALIZADO)
        {
            throw new InvalidOperationException("No se puede editar un pasajero de una ruta en curso o finalizada.");
        }

        pasajero.DireccionRecogida = datos.Direccion.Trim();
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();
    }

    /// <summary>Mueve al pasajero a otra ruta de la misma sede, fecha, hora y tipo, al final de su orden.</summary>
    public async Task MoverAsync(int empresaId, int servicioPasajeroId, MoverServicioPasajeroDto datos)
    {
        var pasajero = await _acceso.ObtenerPasajeroDeLaEmpresaOFallarAsync(empresaId, servicioPasajeroId);
        var servicioActual = await _acceso.ObtenerServicioDeLaEmpresaOFallarAsync(empresaId, pasajero.ServicioId);

        if (servicioActual.Estado is EstadoServicio.CANCELADO or EstadoServicio.EN_CURSO or EstadoServicio.FINALIZADO)
        {
            throw new InvalidOperationException("No se puede mover un pasajero de una ruta cancelada, en curso o finalizada.");
        }

        if (datos.ServicioDestinoId == servicioActual.ServicioId)
        {
            throw new InvalidOperationException("El pasajero ya está en esa ruta.");
        }

        var destino = await _acceso.ObtenerServicioDeLaEmpresaOFallarAsync(empresaId, datos.ServicioDestinoId);
        if (destino.Estado is EstadoServicio.CANCELADO or EstadoServicio.EN_CURSO or EstadoServicio.FINALIZADO)
        {
            throw new InvalidOperationException("La ruta destino está cancelada, en curso o finalizada.");
        }

        if (destino.SedeId != servicioActual.SedeId || destino.Tipo != servicioActual.Tipo
            || destino.Fecha != servicioActual.Fecha || destino.HoraProgramada != servicioActual.HoraProgramada)
        {
            throw new InvalidOperationException("Solo se puede mover un pasajero a otra ruta de la misma sede, fecha, hora y tipo.");
        }

        var pasajerosDestino = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(destino.ServicioId);
        pasajero.ServicioId = destino.ServicioId;
        pasajero.Orden = pasajerosDestino.Count == 0 ? 1 : pasajerosDestino.Max(p => p.Orden) + 1;
        await _servicioPasajeroRepositorio.GuardarCambiosAsync();

        await _aprendizCorredor.AprenderAsync(empresaId, pasajero, destino);

        var pasajerosRestantesEnOrigen = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(servicioActual.ServicioId);
        if (pasajerosRestantesEnOrigen.Count == 0)
        {
            await LiberarServicioVacioAsync(empresaId, servicioActual);
        }
    }

    /// <summary>
    /// Un servicio que se quedó sin pasajeros (porque el último se movió a otra ruta) ya no representa
    /// nada operativo, así que se elimina. Si tenía una unidad operativa asignada, esa unidad queda
    /// libre para la primera ruta de la misma jornada que esté pendiente de conductor (se prefiere una de
    /// la misma sede, fecha, hora y tipo, por si quedó gente sin asignar justo en ese horario), pero nunca
    /// una cuya fecha y hora coincidan con otra ruta que esa misma unidad ya tenga en la jornada (un
    /// conductor no puede estar en dos rutas a la vez, ver <see cref="ReglasUnidadOperativa.HayConflictoTemporal"/>);
    /// si no hay ninguna ruta pendiente compatible, el conductor simplemente queda disponible para una futura importación.
    /// </summary>
    private async Task LiberarServicioVacioAsync(int empresaId, Servicio servicioVacio)
    {
        var unidadLiberada = servicioVacio.UnidadOperativaId;
        var jornadaId = servicioVacio.JornadaId;

        await _servicioRepositorio.EliminarAsync(servicioVacio);
        await _servicioRepositorio.GuardarCambiosAsync();

        if (unidadLiberada is null)
        {
            return;
        }

        var serviciosDeLaJornada = await _servicioRepositorio.ObtenerPorJornadaAsync(jornadaId);

        var rutasQueOcupanALaUnidad = serviciosDeLaJornada.Where(s => s.UnidadOperativaId == unidadLiberada).ToList();

        var pendientesDeConductor = serviciosDeLaJornada
            .Where(s => s.UnidadOperativaId is null && s.Estado == EstadoServicio.PENDIENTE_ASIGNACION
                && !ReglasUnidadOperativa.HayConflictoTemporal(s, rutasQueOcupanALaUnidad))
            .ToList();

        var candidato = pendientesDeConductor.FirstOrDefault(s =>
            s.SedeId == servicioVacio.SedeId && s.Tipo == servicioVacio.Tipo && s.Fecha == servicioVacio.Fecha && s.HoraProgramada == servicioVacio.HoraProgramada)
            ?? pendientesDeConductor.OrderBy(s => s.HoraProgramada).FirstOrDefault();

        if (candidato is null)
        {
            return;
        }

        await _servicioServicio.AsignarUnidadAsync(empresaId, candidato.ServicioId, new AsignarUnidadServicioDto { UnidadOperativaId = unidadLiberada.Value });
    }
}
