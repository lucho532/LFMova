namespace LFMova.Application.Interfaces;

/// <summary>
/// Persistencia del borrado de una persona: consultas de lo que depende de
/// ella y borrado en cascada dentro de una transacción. No decide si la
/// persona puede eliminarse ni con qué alcance: eso lo resuelve
/// <see cref="IEliminacionPersonaServicio"/>.
/// </summary>
public interface IEliminacionPersonaRepositorio
{
    /// <summary>Indica si la persona es conductora o pasajera de una ruta que está en curso.</summary>
    Task<bool> TieneRutaEnCursoAsync(int usuarioId);

    /// <summary>Cuenta cuántas personas distintas de la indicada tienen activo el rol de administrador de plataforma.</summary>
    Task<int> ContarOtrosAdministradoresAsync(int usuarioId);

    /// <summary>
    /// Empresas con las que la persona tiene relación: por un rol, como
    /// empleada, como conductora vinculada o por una invitación aceptada.
    /// </summary>
    Task<List<int>> ObtenerEmpresasRelacionadasAsync(int usuarioId);

    /// <summary>
    /// Borra la cuenta y todo lo que depende de ella. Las rutas que conducía
    /// quedan sin unidad asignada; las importaciones que registró y las
    /// invitaciones que envió como coordinadora se conservan, sin autor.
    /// Devuelve las referencias de los archivos de evidencia que quedaron
    /// huérfanos, para borrarlos del almacenamiento.
    /// </summary>
    Task<List<string>> EliminarCuentaAsync(int usuarioId);

    /// <summary>
    /// Quita a la persona de una sola empresa: su ficha de empleado con su
    /// historial como pasajera, su vinculación como conductora y las
    /// invitaciones de esa empresa. La cuenta se conserva. Devuelve las
    /// referencias de los archivos de evidencia que quedaron huérfanos.
    /// </summary>
    Task<List<string>> QuitarDeEmpresaAsync(int usuarioId, int empresaId);
}
