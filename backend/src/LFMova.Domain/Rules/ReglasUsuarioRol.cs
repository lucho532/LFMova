using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.Domain.Rules;

/// <summary>
/// Reglas de dominio para la asignación y consistencia de
/// <c>UsuarioRol</c>. No implementan autenticación ni políticas de
/// autorización de ASP.NET Core (eso corresponde a una tarea posterior);
/// expresan únicamente las reglas de negocio sobre la forma y unicidad de
/// los roles. No acceden a persistencia.
/// </summary>
public static class ReglasUsuarioRol
{
    /// <summary>
    /// Indica si asignar el rol <c>COORDINADOR</c> en la empresa indicada
    /// violaría la unicidad de empresa para ese rol, dado el conjunto de
    /// roles activos que ya tiene el usuario.
    /// </summary>
    public static bool ViolaUnicidadDeCoordinador(
        IEnumerable<UsuarioRol> rolesActivosDelUsuario,
        int nuevaEmpresaId)
        => rolesActivosDelUsuario.Any(r =>
            r.Rol == Rol.COORDINADOR && r.Activo && r.EmpresaId != nuevaEmpresaId);

    /// <summary>
    /// Indica si una operación de asignación de rol constituye una
    /// autoasignación no permitida (el usuario ejecutor y el usuario
    /// destino son la misma persona).
    /// </summary>
    public static bool EsAutoasignacion(int usuarioEjecutorId, int usuarioDestinoId)
        => usuarioEjecutorId == usuarioDestinoId;

    /// <summary>
    /// Indica si, dado el conjunto de roles activos que ya tiene el usuario,
    /// este ya es <c>COORDINADOR</c> activo de la empresa indicada.
    /// </summary>
    public static bool YaEsCoordinadorDeEmpresa(
        IEnumerable<UsuarioRol> rolesActivosDelUsuario,
        int empresaId)
        => rolesActivosDelUsuario.Any(r =>
            r.Rol == Rol.COORDINADOR && r.Activo && r.EmpresaId == empresaId);

    /// <summary>
    /// Indica si el <c>UsuarioRol</c> a revocar es el último
    /// <c>COORDINADOR</c> activo de la empresa, dado el conjunto de roles
    /// <c>COORDINADOR</c> de esa empresa. Una empresa debe conservar siempre
    /// al menos un coordinador activo.
    /// </summary>
    public static bool EsElUltimoCoordinadorActivo(
        IEnumerable<UsuarioRol> rolesCoordinadorDeLaEmpresa,
        int usuarioRolIdARevocar)
    {
        var activos = rolesCoordinadorDeLaEmpresa.Where(r => r.Rol == Rol.COORDINADOR && r.Activo).ToList();
        return activos.Count == 1 && activos[0].UsuarioRolId == usuarioRolIdARevocar;
    }
}
