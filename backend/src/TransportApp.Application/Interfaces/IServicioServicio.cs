using TransportApp.Application.DTOs.Servicios;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define los casos de uso de creación y gestión del ciclo de vida de
/// servicios de una jornada. No decide autorización: eso se verifica en la
/// capa de Api antes de invocar estos métodos.
/// </summary>
public interface IServicioServicio
{
    /// <summary>
    /// Crea un nuevo servicio para la jornada indicada, en estado
    /// <c>BORRADOR</c>. Valida que la jornada y la sede pertenezcan a la
    /// empresa indicada.
    /// </summary>
    Task<ServicioDto> CrearAsync(int empresaId, int jornadaId, CrearServicioDto datos);

    /// <summary>
    /// Obtiene un servicio por su identificador, validando que pertenezca
    /// (a través de su jornada) a la empresa indicada. Devuelve <c>null</c>
    /// si no existe o no pertenece a esa empresa.
    /// </summary>
    Task<ServicioDto?> ObtenerPorIdAsync(int empresaId, int servicioId);

    /// <summary>Obtiene los servicios de la jornada indicada.</summary>
    Task<List<ServicioDto>> ObtenerPorJornadaAsync(int empresaId, int jornadaId);

    /// <summary>
    /// Servicios que la pantalla de Programación debe mostrar, de todas las
    /// jornadas de la empresa: los que todavía no se envían (borrador,
    /// pendiente de asignación o asignado), sin importar su fecha, y los
    /// publicados o en curso con fecha desde <paramref name="desde"/>. Nunca
    /// los finalizados ni los cancelados.
    /// </summary>
    Task<List<ServicioDto>> ObtenerPendientesDeProgramacionAsync(int empresaId, DateOnly desde);

    /// <summary>
    /// Cambia el estado del servicio indicado, validando que la transición
    /// sea válida según <see cref="Domain.Rules.ReglasEstadoServicio"/>.
    /// </summary>
    Task CambiarEstadoAsync(int empresaId, int servicioId, CambiarEstadoServicioDto datos);

    /// <summary>
    /// Asigna o reasigna la unidad operativa del servicio. Modifica
    /// exclusivamente <c>Servicio.UnidadOperativaId</c>; nunca cambia
    /// <c>Servicio.JornadaId</c>. Valida que la unidad esté activa, que su
    /// conductor tenga una vinculación activa con la empresa, y que no exista
    /// conflicto temporal con otro servicio ya asignado a esa unidad. Si la
    /// reasignación cambia efectivamente el conductor responsable, notifica
    /// al nuevo conductor y a cada empleado participante; el conductor
    /// anterior no se notifica (ver <c>data-model.md</c> §23 y <c>tasks.md</c>
    /// T074).
    /// </summary>
    Task AsignarUnidadAsync(int empresaId, int servicioId, AsignarUnidadServicioDto datos);

    /// <summary>
    /// "Despublica" una ruta ya enviada (<c>PUBLICADO → ASIGNADO</c>) para
    /// que el coordinador pueda seguir editándola (por ejemplo, agregarle un
    /// pasajero de última hora arrastrándolo o asignándolo directo).
    /// Conserva la unidad operativa asignada. Si tiene conductor, se le
    /// notifica que su ruta volvió a edición. Falla si el servicio no está
    /// en estado <c>PUBLICADO</c>.
    /// </summary>
    Task DespublicarAsync(int empresaId, int servicioId);

    /// <summary>
    /// Elimina por completo la ruta (borrado físico, no un cambio de
    /// estado): borra sus <c>ServicioPasajero</c> (las <c>ProgramacionTransporte</c>
    /// correspondientes quedan libres, sin asignar) y luego el propio
    /// servicio. Si tenía conductor, se le notifica. Falla si el servicio
    /// está <c>EN_CURSO</c> o <c>FINALIZADO</c>: esos representan operación
    /// real que no se puede deshacer.
    /// </summary>
    Task EliminarAsync(int empresaId, int servicioId);

    /// <summary>
    /// Obtiene el <c>UsuarioId</c> del conductor de la unidad operativa
    /// asignada al servicio, o <c>null</c> si el servicio no existe en la
    /// empresa o no tiene unidad asignada. Se usa desde la capa de Api para
    /// verificar que solo el conductor responsable pueda iniciar su propio
    /// servicio (ver <c>tasks.md</c> T083).
    /// </summary>
    Task<int?> ObtenerUsuarioIdConductorAsignadoAsync(int empresaId, int servicioId);

    /// <summary>
    /// Inicia manualmente la ejecución del servicio: lo pasa a
    /// <c>EN_CURSO</c> y registra <c>HoraInicioReal</c> (ver <c>spec.md</c>
    /// §24 y <c>tasks.md</c> T083), junto con la ubicación real del
    /// dispositivo del conductor en ese momento (<paramref name="datos"/>,
    /// opcional: si el dispositivo no la entrega, se inicia igual). Falla si
    /// el servicio no está en estado <c>PUBLICADO</c>.
    /// </summary>
    Task IniciarAsync(int empresaId, int servicioId, IniciarServicioDto? datos = null);

    /// <summary>
    /// Finaliza manualmente la ejecución del servicio: lo pasa a
    /// <c>FINALIZADO</c> y registra <c>HoraFinReal</c> (ver <c>spec.md</c>
    /// §27 y <c>tasks.md</c> T090), junto con la ubicación real del
    /// dispositivo del conductor en ese momento (<paramref name="datos"/>,
    /// opcional: si el dispositivo no la entrega, se finaliza igual). Para
    /// <c>ENTRADA</c> no se permite finalizar si existen pasajeros pendientes
    /// de procesar (ver
    /// <see cref="Domain.Rules.ReglasEstadoServicioPasajero.EstaProcesado"/>).
    /// Para <c>SALIDA</c> no se exigen pasajeros procesados.
    /// Falla si el servicio no está en estado <c>EN_CURSO</c>.
    /// </summary>
    Task FinalizarAsync(int empresaId, int servicioId, FinalizarServicioDto? datos = null);
}
