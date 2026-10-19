using LFMova.Application.DTOs.Empleados;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Caso de uso de eliminar a una persona desde la gestión de empleados de una
/// empresa (decisión del 2026-10-02). Sirve para corregir registros erróneos
/// y para que la persona pueda volver a registrarse sin conflictos. No
/// desactiva ni archiva: borra, incluido el historial de la persona.
/// </summary>
public interface IEliminacionPersonaServicio
{
    /// <summary>
    /// Elimina al empleado indicado de la empresa. Si la persona solo
    /// pertenece a esta empresa se borra su cuenta completa (usuario, roles,
    /// ficha de conductor con sus vehículos, rutas en las que fue pasajera,
    /// mensajes, incidencias, invitaciones); si también pertenece a otra, solo
    /// se la quita de esta. Falla si el empleado no es de la empresa, si es
    /// quien hace la solicitud, si es coordinador o administrador, o si tiene
    /// una ruta en curso.
    /// </summary>
    Task<ResultadoEliminacionPersonaDto> EliminarAsync(int empresaId, int empleadoId, int usuarioSolicitanteId);

    /// <summary>
    /// La propia persona elimina su cuenta completa, sea cual sea su rol
    /// (decisión del 2026-10-03): se borra todo rastro suyo, igual que en
    /// <see cref="EliminarAsync"/>. Exige su contraseña actual. Falla si la
    /// contraseña no coincide, si tiene una ruta en curso o si es la única
    /// administradora de la plataforma.
    /// </summary>
    Task EliminarPropiaCuentaAsync(int usuarioId, string contrasena);
}
