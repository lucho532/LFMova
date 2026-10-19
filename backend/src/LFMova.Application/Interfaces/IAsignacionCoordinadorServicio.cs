using LFMova.Application.DTOs.Empresas;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define los casos de uso sobre coordinadores adicionales de una empresa ya
/// existente: consulta, asignación, revocación y reactivación. No decide qué
/// usuarios están autorizados a invocar estas operaciones (rol y contexto de
/// empresa del ejecutor): eso se verifica en la capa de Api antes de llamar
/// a estos métodos. La creación del primer coordinador ocurre junto con la
/// empresa, en <see cref="IEmpresaServicio.CrearAsync"/>.
/// </summary>
public interface IAsignacionCoordinadorServicio
{
    /// <summary>Obtiene todos los coordinadores (activos e inactivos) de la empresa indicada.</summary>
    Task<List<CoordinadorDto>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>
    /// Asigna el rol <c>COORDINADOR</c> de la empresa indicada al usuario
    /// identificado por su cédula (creándolo, pendiente de activación, si
    /// todavía no existe). Valida que no sea una autoasignación, que el
    /// usuario destino no sea ya coordinador de otra empresa y que no lo sea
    /// ya de esta misma empresa.
    /// </summary>
    Task AsignarAsync(int empresaId, AsignarCoordinadorDto datos, int usuarioEjecutorId);

    /// <summary>
    /// Revoca (desactiva) el <c>UsuarioRol COORDINADOR</c> indicado,
    /// perteneciente a la empresa indicada. Rechaza la operación si dejaría
    /// a la empresa sin ningún coordinador activo.
    /// </summary>
    Task RevocarAsync(int empresaId, int usuarioRolId);

    /// <summary>
    /// Reactiva un <c>UsuarioRol COORDINADOR</c> previamente revocado,
    /// perteneciente a la empresa indicada. Rechaza la operación si el
    /// usuario ya es coordinador activo de otra empresa distinta.
    /// </summary>
    Task ReactivarAsync(int empresaId, int usuarioRolId);
}
