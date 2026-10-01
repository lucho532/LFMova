using LFMova.Application.DTOs.Jornadas;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define los casos de uso de administración de jornadas de una empresa. No
/// decide autorización: eso se verifica en la capa de Api antes de invocar
/// estos métodos.
/// </summary>
public interface IJornadaServicio
{
    /// <summary>
    /// Crea una nueva jornada para la empresa indicada. Valida que la unidad
    /// operativa exista y que su conductor tenga una vinculación activa con
    /// esa empresa.
    /// </summary>
    Task<JornadaDto> CrearAsync(int empresaId, CrearJornadaDto datos);

    /// <summary>Obtiene todas las jornadas de la empresa indicada.</summary>
    Task<List<JornadaDto>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>
    /// Publica la jornada: pasa a <c>PUBLICADO</c> todos sus servicios en
    /// estado <c>ASIGNADO</c>. Falla si algún servicio de la jornada está en
    /// <c>BORRADOR</c> o <c>PENDIENTE_ASIGNACION</c> (ver <c>spec.md</c> §20).
    /// Notifica al conductor y a cada empleado participante de cada servicio
    /// publicado (ver <c>tasks.md</c> T073).
    /// </summary>
    Task PublicarAsync(int empresaId, int jornadaId);

    /// <summary>
    /// Deshace el reparto automático de la jornada: elimina por completo los
    /// servicios que todavía no se publicaron (<c>BORRADOR</c>,
    /// <c>PENDIENTE_ASIGNACION</c> o <c>ASIGNADO</c>) junto con sus
    /// pasajeros, dejando libres las programaciones de transporte para
    /// repartirlas de nuevo. No toca servicios ya <c>PUBLICADO</c>,
    /// <c>EN_CURSO</c>, <c>FINALIZADO</c> ni <c>CANCELADO</c>: esos ya son
    /// operación real y no se pueden deshacer.
    /// </summary>
    Task<DeshacerRepartoDto> DeshacerRepartoAsync(int empresaId, int jornadaId);

    /// <summary>
    /// Elimina todo rastro de programación de la jornada para poder volver a
    /// importar el Excel de esa fecha desde cero: además de lo que ya hace
    /// <see cref="DeshacerRepartoAsync"/> (servicios y pasajeros sin
    /// publicar), también elimina las <c>ProgramacionTransporte</c> de esa
    /// fecha que hayan quedado sin asignar (por ejemplo, filas que una
    /// importación fallida dejó a medias) y, si la jornada queda sin ningún
    /// servicio, la jornada misma. Nunca toca <c>Empleado</c> ni <c>Usuario</c>
    /// (identidad global del empleado, ver <c>AGENTS.md</c> §15) ni el
    /// registro de <c>ImportacionExcel</c> (auditoría, ver §27). Falla si la
    /// jornada tiene algún servicio <c>PUBLICADO</c>, <c>EN_CURSO</c> o
    /// <c>FINALIZADO</c>: eso ya es operación real y no se puede deshacer.
    /// </summary>
    Task<EliminarRastroDto> EliminarRastroAsync(int empresaId, int jornadaId);
}
