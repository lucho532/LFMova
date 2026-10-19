using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.Domain.Rules;

/// <summary>
/// Reglas de qué cuenta como asistencia en una ruta y en qué orden se presentan los pasajeros en un
/// informe. No persiste ni consulta datos. Debe coincidir con el filtro y el orden del detalle de ruta
/// del frontend (<c>ordenRuta.ts</c>).
/// </summary>
public static class ReglasAsistenciaRuta
{
    /// <summary>Indica si un pasajero con ese estado entra en el filtro. Los pendientes y cancelados solo salen con <see cref="FiltroAsistenciaRuta.TODOS"/>.</summary>
    public static bool Cumple(EstadoServicioPasajero estado, FiltroAsistenciaRuta filtro) => filtro switch
    {
        FiltroAsistenciaRuta.ASISTIERON => estado == EstadoServicioPasajero.RECOGIDO,
        FiltroAsistenciaRuta.NO_ASISTIERON => estado is EstadoServicioPasajero.NO_RECOGIDO or EstadoServicioPasajero.NO_ASISTIRA,
        _ => true
    };

    /// <summary>
    /// Orden cronológico de la ruta: primero quienes ya tienen resultado registrado, por la hora en que se
    /// registró; después los que no llegaron a procesarse, por su orden de recogida.
    /// </summary>
    public static List<ServicioPasajero> OrdenarCronologicamente(IEnumerable<ServicioPasajero> pasajeros)
    {
        var lista = pasajeros.ToList();
        var procesados = lista.Where(p => p.HoraProcesado is not null).OrderBy(p => p.HoraProcesado).ThenBy(p => p.Orden);
        var sinProcesar = lista.Where(p => p.HoraProcesado is null).OrderBy(p => p.Orden);
        return procesados.Concat(sinProcesar).ToList();
    }
}
