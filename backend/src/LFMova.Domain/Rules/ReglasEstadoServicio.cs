using LFMova.Domain.Enums;

namespace LFMova.Domain.Rules;

/// <summary>
/// Regla de dominio que define las transiciones válidas del ciclo de vida de
/// un <c>Servicio</c> (ver <c>spec.md</c> §18-19). No accede a persistencia
/// ni decide autorización; solo determina si un cambio de estado es
/// estructuralmente válido.
/// </summary>
public static class ReglasEstadoServicio
{
    private static readonly Dictionary<EstadoServicio, EstadoServicio[]> TransicionesValidas = new()
    {
        [EstadoServicio.BORRADOR] = [EstadoServicio.PENDIENTE_ASIGNACION, EstadoServicio.CANCELADO],
        [EstadoServicio.PENDIENTE_ASIGNACION] = [EstadoServicio.ASIGNADO, EstadoServicio.CANCELADO],
        [EstadoServicio.ASIGNADO] = [EstadoServicio.PUBLICADO, EstadoServicio.PENDIENTE_ASIGNACION, EstadoServicio.CANCELADO],
        [EstadoServicio.PUBLICADO] = [EstadoServicio.EN_CURSO, EstadoServicio.CANCELADO, EstadoServicio.ASIGNADO],
        [EstadoServicio.EN_CURSO] = [EstadoServicio.FINALIZADO],
        [EstadoServicio.FINALIZADO] = [],
        [EstadoServicio.CANCELADO] = []
    };

    /// <summary>
    /// Indica si es válido pasar del estado actual al estado indicado. La
    /// transición <c>ASIGNADO → PENDIENTE_ASIGNACION</c> es válida porque
    /// representa la retirada de una unidad para reasignar el servicio (ver
    /// <c>tasks.md</c> T067). La transición <c>PUBLICADO → ASIGNADO</c>
    /// representa que el coordinador "despublica" una ruta ya enviada para
    /// poder seguir editándola (por ejemplo, para agregarle un pasajero de
    /// última hora); conserva el conductor asignado. Un servicio
    /// <c>EN_CURSO</c>, <c>FINALIZADO</c> o <c>CANCELADO</c> no admite más
    /// transiciones.
    /// </summary>
    public static bool EsTransicionValida(EstadoServicio actual, EstadoServicio nuevo)
        => TransicionesValidas.TryGetValue(actual, out var permitidos) && permitidos.Contains(nuevo);

    /// <summary>
    /// Indica si un servicio ya es visible para su conductor y sus pasajeros:
    /// solo desde que el coordinador lo publicó (<c>PUBLICADO</c>), durante su
    /// ejecución y una vez finalizado. Los borradores, pendientes, asignados
    /// sin publicar y cancelados son de trabajo interno del coordinador.
    /// </summary>
    public static bool EsVisibleParaConductorYEmpleado(EstadoServicio estado)
        => estado is EstadoServicio.PUBLICADO or EstadoServicio.EN_CURSO or EstadoServicio.FINALIZADO;

    /// <summary>
    /// Estado en que queda un servicio al perder su unidad operativa (por
    /// ejemplo, al eliminarse su conductor): uno asignado o publicado vuelve
    /// a <c>PENDIENTE_ASIGNACION</c>; el resto conserva su estado.
    /// </summary>
    public static EstadoServicio EstadoAlQuedarSinUnidad(EstadoServicio estado)
        => estado is EstadoServicio.ASIGNADO or EstadoServicio.PUBLICADO ? EstadoServicio.PENDIENTE_ASIGNACION : estado;

    /// <summary>
    /// Indica si a un servicio en este estado se le pueden sumar pasajeros
    /// nuevos: no a uno que ya está en curso, finalizado o cancelado (el
    /// conductor ya salió o la ruta ya no existe).
    /// </summary>
    public static bool AdmitePasajerosNuevos(EstadoServicio estado)
        => estado is not (EstadoServicio.EN_CURSO or EstadoServicio.FINALIZADO or EstadoServicio.CANCELADO);
}
