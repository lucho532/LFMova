using TransportApp.Domain.Enums;

namespace TransportApp.Domain.Rules;

/// <summary>
/// Reglas de dominio del procesamiento de un <c>ServicioPasajero</c> durante
/// la ejecución (ver <c>spec.md</c> §22 y <c>tasks.md</c> T085/T090). El
/// conductor registra la llegada y luego el resultado: <c>RECOGIDO</c> con un
/// botón, o una incidencia que impide la recogida (no contesta, no se
/// encuentra, dirección incorrecta, no se pudo recoger), que deja al pasajero
/// como <c>NO_RECOGIDO</c>. No accede a persistencia ni decide autorización.
/// </summary>
public static class ReglasEstadoServicioPasajero
{
    /// <summary>
    /// Indica si es válido registrar manualmente el resultado <paramref name="nuevo"/>
    /// desde <paramref name="actual"/>: solo <c>CONDUCTOR_LLEGO → RECOGIDO</c>.
    /// </summary>
    public static bool EsTransicionDeResultadoValida(EstadoServicioPasajero actual, EstadoServicioPasajero nuevo)
        => actual == EstadoServicioPasajero.CONDUCTOR_LLEGO && nuevo == EstadoServicioPasajero.RECOGIDO;

    /// <summary>
    /// Indica si un tipo de incidencia significa que no se pudo recoger al
    /// pasajero (y por tanto lo deja como <c>NO_RECOGIDO</c>). <c>UBICACION_MODIFICADA</c>
    /// y <c>OTRA</c> son informativas y no cambian su estado.
    /// </summary>
    public static bool IncidenciaImpideLaRecogida(TipoIncidencia tipo)
        => tipo is TipoIncidencia.NO_CONTESTA
            or TipoIncidencia.NO_SE_ENCUENTRA
            or TipoIncidencia.DIRECCION_INCORRECTA
            or TipoIncidencia.NO_SE_PUDO_RECOGER;

    /// <summary>
    /// Indica si el pasajero aún puede quedar como <c>NO_RECOGIDO</c> por una
    /// incidencia (todavía no se le dio un resultado).
    /// </summary>
    public static bool PuedeQuedarNoRecogido(EstadoServicioPasajero estado)
        => estado is EstadoServicioPasajero.PROGRAMADO
            or EstadoServicioPasajero.CONFIRMADO
            or EstadoServicioPasajero.CONDUCTOR_LLEGO;

    /// <summary>
    /// Indica si el pasajero ya fue procesado (no requiere más acción del
    /// conductor durante la ejecución del servicio): fue recogido, no se pudo
    /// recoger, avisó que no asistirá o su participación se canceló (ver
    /// <c>tasks.md</c> T090, "validar pasajeros procesados").
    /// </summary>
    public static bool EstaProcesado(EstadoServicioPasajero estado)
        => estado is EstadoServicioPasajero.RECOGIDO
            or EstadoServicioPasajero.NO_RECOGIDO
            or EstadoServicioPasajero.NO_ASISTIRA
            or EstadoServicioPasajero.CANCELADO;
}
