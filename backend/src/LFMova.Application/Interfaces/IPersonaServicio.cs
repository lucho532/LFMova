using LFMova.Application.DTOs.Personas;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define la búsqueda de una persona registrada por su cédula, para que un
/// administrador o coordinador verifique quién es antes de asignarle un rol,
/// y la regla de qué personas puede gestionar un coordinador: solo las que
/// ya forman parte de su empresa (empleados, conductores vinculados,
/// coordinadores o quienes aceptaron una invitación). No decide
/// autorización por rol: eso se verifica en la capa de Api.
/// </summary>
public interface IPersonaServicio
{
    /// <summary>
    /// Busca a la persona por cédula. Si se indica <paramref name="empresaIdRestringida"/>
    /// (búsqueda hecha por un coordinador), solo la encuentra si está
    /// relacionada con esa empresa; si no, falla igual que si la cédula no
    /// existiera, para no revelar datos de personas ajenas a la empresa.
    /// </summary>
    Task<PersonaDto> BuscarPorCedulaAsync(string cedula, int? empresaIdRestringida = null);

    /// <summary>Indica si existe una cuenta con esa cédula y ya forma parte de la empresa.</summary>
    Task<bool> EstaRelacionadaConEmpresaAsync(string cedula, int empresaId);

    /// <summary>
    /// Indica si un coordinador de la empresa puede asignarle un rol a la
    /// persona con esa cédula: cuando todavía no existe ninguna cuenta (se
    /// crea por invitación) o cuando ya forma parte de la empresa. Una
    /// persona registrada que no es parte de la empresa primero debe
    /// aceptar una invitación.
    /// </summary>
    Task<bool> PuedeGestionarseDesdeEmpresaAsync(string cedula, int empresaId);
}
