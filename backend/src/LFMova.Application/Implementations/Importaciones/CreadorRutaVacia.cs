using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.DTOs.Jornadas;
using LFMova.Application.DTOs.Servicios;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Abre a mano una ruta sin pasajeros para una unidad operativa, reutilizando la jornada de la fecha y
/// la ruta que esa unidad ya tenga en ese mismo horario. No agrega pasajeros ni decide autorización.
/// </summary>
public class CreadorRutaVacia
{
    private readonly IJornadaServicio _jornadaServicio;
    private readonly IServicioServicio _servicioServicio;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public CreadorRutaVacia(IJornadaServicio jornadaServicio, IServicioServicio servicioServicio)
    {
        _jornadaServicio = jornadaServicio;
        _servicioServicio = servicioServicio;
    }

    /// <summary>Crea la ruta vacía (o reutiliza la existente) y devuelve su jornada y su servicio.</summary>
    public async Task<ResultadoImportacionDto> CrearAsync(int empresaId, CrearRutaVaciaDto datos)
    {
        // Sirve para dividir una ruta sobrecargada: primero se abre esta ruta vacía para un conductor
        // libre en el mismo horario, y luego se le mueven pasajeros desde la ruta original (ver
        // ServicioPasajeroServicio.MoverAsync), sin tener que dar de alta a nadie por acá.
        var (resultado, _) = await ObtenerOCrearRutaAsignadaAsync(
            empresaId, datos.Fecha, datos.Hora, datos.Tipo, datos.SedeId, datos.UnidadOperativaId,
            "No se puede usar una ruta cancelada, en curso o finalizada.");
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
}
