using TransportApp.Application.DTOs.ServiciosPasajero;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define los casos de uso de asignación de pasajeros a servicios y de
/// gestión de su participación (confirmación, reordenamiento). No decide
/// autorización: eso se verifica en la capa de Api antes de invocar estos
/// métodos.
/// </summary>
public interface IServicioPasajeroServicio
{
    /// <summary>
    /// Asigna la programación indicada al servicio, creando su
    /// <c>ServicioPasajero</c> en estado <c>PROGRAMADO</c>. Falla si la
    /// programación no pertenece a la misma empresa, si su sede no coincide
    /// con la del servicio, o si ya está asignada a otro servicio.
    /// </summary>
    Task<ServicioPasajeroDto> CrearAsync(int empresaId, int servicioId, CrearServicioPasajeroDto datos);

    /// <summary>Obtiene los pasajeros del servicio indicado.</summary>
    Task<List<ServicioPasajeroDto>> ObtenerPorServicioAsync(int empresaId, int servicioId);

    /// <summary>
    /// Confirma la asistencia del pasajero. Si se proporciona una nueva
    /// dirección y se solicita establecerla como habitual, actualiza
    /// <c>Empleado.Direccion</c> y conserva la anterior en
    /// <c>UbicacionRecogidaHistorica</c> (ver <c>tasks.md</c> T072A).
    /// </summary>
    Task ConfirmarAsync(int empresaId, int servicioPasajeroId, ConfirmarServicioPasajeroDto datos);

    /// <summary>
    /// Marca que el pasajero no asistirá: notifica al conductor responsable
    /// del servicio y deja al pasajero deshabilitado operacionalmente, sin
    /// impedir la finalización del servicio.
    /// </summary>
    Task MarcarNoAsistiraAsync(int empresaId, int servicioPasajeroId);

    /// <summary>Modifica el orden operativo del pasajero dentro del servicio.</summary>
    Task ReordenarAsync(int empresaId, int servicioPasajeroId, ReordenarServicioPasajeroDto datos);

    /// <summary>
    /// Obtiene el <c>UsuarioId</c> del conductor de la unidad operativa
    /// asignada al servicio del pasajero indicado, o <c>null</c> si no puede
    /// resolverse. Se resuelve a partir del propio pasajero (no de un
    /// <c>servicioId</c> recibido por separado) para evitar que un conductor
    /// autorizado para un pasajero actúe sobre el pasajero de otro servicio
    /// (ver <c>tasks.md</c> T096).
    /// </summary>
    Task<int?> ObtenerUsuarioIdConductorAsync(int empresaId, int servicioPasajeroId);

    /// <summary>
    /// Registra manualmente que el conductor llegó al punto de recogida del
    /// pasajero (ver <c>spec.md</c> §24/§26 y <c>tasks.md</c> T084). Es la
    /// alternativa manual mientras la detección automática por
    /// geolocalización permanezca pendiente de definición (radio de geocerca
    /// y proveedor tecnológico). Falla si el servicio no está <c>EN_CURSO</c>
    /// o si el pasajero no está en un estado que admita esta transición.
    /// </summary>
    Task MarcarLlegadaAsync(int empresaId, int servicioPasajeroId);

    /// <summary>
    /// Registra que el pasajero fue recogido tras la llegada del conductor
    /// (<c>CONDUCTOR_LLEGO → RECOGIDO</c>). Los casos en que no se recoge se
    /// registran como incidencia, que deja al pasajero como <c>NO_RECOGIDO</c> (ver
    /// <c>tasks.md</c> T085/T090). No genera incidencias automáticamente: la
    /// gestión de incidencias es manual (Fase 16). Falla si la transición no
    /// es válida según <see cref="Domain.Rules.ReglasEstadoServicioPasajero"/>.
    /// </summary>
    Task CambiarEstadoAsync(int empresaId, int servicioPasajeroId, CambiarEstadoServicioPasajeroDto datos);

    /// <summary>
    /// Obtiene el <c>UsuarioId</c> del empleado dueño del pasajero indicado,
    /// o <c>null</c> si no existe en la empresa. Se usa desde la capa de Api
    /// para verificar que solo ese empleado pueda compartir su ubicación
    /// (ver <c>tasks.md</c> T088).
    /// </summary>
    Task<int?> ObtenerUsuarioIdEmpleadoAsync(int empresaId, int servicioPasajeroId);

    /// <summary>
    /// Actualiza la ubicación compartida por el empleado para este servicio
    /// (ver <c>spec.md</c> §23 y <c>tasks.md</c> T088). Es la ubicación
    /// exacta utilizada para este servicio específico; no modifica la
    /// dirección habitual del empleado.
    /// </summary>
    Task CompartirUbicacionAsync(int empresaId, int servicioPasajeroId, CompartirUbicacionDto datos);

    /// <summary>
    /// Obtiene los servicios (como pasajero) del empleado asociado al usuario
    /// indicado, en cualquier empresa, más recientes primero. Falla si el
    /// usuario autenticado no tiene un perfil de empleado.
    /// </summary>
    Task<List<ServicioDelEmpleadoDto>> ObtenerPorUsuarioEmpleadoAsync(int usuarioId);

    /// <summary>
    /// Guarda el punto GPS exacto de la recogida (lo registra el conductor) y
    /// lo conserva también como ubicación histórica del empleado.
    /// </summary>
    Task GuardarUbicacionRecogidaAsync(int empresaId, int servicioPasajeroId, CompartirUbicacionDto datos);

    /// <summary>Devuelve las ubicaciones de recogida anteriores del empleado del pasajero, la más reciente primero.</summary>
    Task<List<UbicacionAnteriorDto>> ObtenerUbicacionesAnterioresAsync(int empresaId, int servicioPasajeroId);

    /// <summary>
    /// Olvida la ubicación guardada del empleado del pasajero (borra todo su
    /// historial de <c>UbicacionRecogidaHistorica</c>), para que la próxima
    /// vez que se lo recoja se guarde una nueva desde cero.
    /// </summary>
    Task EliminarUbicacionGuardadaAsync(int empresaId, int servicioPasajeroId);

    /// <summary>
    /// El coordinador cancela la participación del pasajero en la ruta
    /// (queda con estado <c>CANCELADO</c>, visible en el historial de esa
    /// ruta). Falla si el pasajero ya tiene un resultado final (recogido, no
    /// recogido, no asistirá o ya estaba cancelado).
    /// </summary>
    Task CancelarAsync(int empresaId, int servicioPasajeroId);

    /// <summary>
    /// El coordinador elimina por completo al pasajero de la ruta (borrado
    /// físico, para cuando se agregó por error): no queda registro de que
    /// estuvo ahí. Falla si el servicio ya está en curso o finalizado.
    /// </summary>
    Task EliminarAsync(int empresaId, int servicioPasajeroId);

    /// <summary>
    /// El coordinador corrige la dirección de recogida de este servicio
    /// puntual (por ejemplo, un dato mal pegado al crear la ruta). No cambia
    /// el estado del pasajero ni notifica a nadie. Falla si el servicio ya
    /// está en curso o finalizado.
    /// </summary>
    Task EditarDireccionAsync(int empresaId, int servicioPasajeroId, EditarDireccionServicioPasajeroDto datos);

    /// <summary>
    /// Mueve al pasajero a la ruta (servicio) de otra unidad operativa con la
    /// misma sede, fecha, hora y tipo; si esa ruta no existe todavía, la crea.
    /// El servicio de origen y el de destino deben pertenecer a la misma
    /// empresa y no estar cancelados, en curso ni finalizados.
    /// </summary>
    Task ReasignarAsync(int empresaId, int servicioPasajeroId, ReasignarServicioPasajeroDto datos);

    /// <summary>
    /// Mueve al pasajero directamente al servicio (ruta) indicado, para
    /// cuando el coordinador lo arrastra de una tarjeta a otra en el reparto.
    /// El destino debe tener la misma sede, fecha, hora y tipo que el
    /// servicio de origen (es la misma necesidad de transporte, repartida en
    /// otra ruta): no permite mover a un horario distinto. Ninguno de los dos
    /// servicios puede estar cancelado, en curso ni finalizado. Si el destino
    /// ya tiene una unidad operativa asignada, no deja superar su capacidad.
    /// </summary>
    Task MoverAsync(int empresaId, int servicioPasajeroId, MoverServicioPasajeroDto datos);
}
