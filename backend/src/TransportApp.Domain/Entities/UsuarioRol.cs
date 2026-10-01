using TransportApp.Domain.Enums;

namespace TransportApp.Domain.Entities;

/// <summary>
/// Representa un rol que un <c>Usuario</c> tiene dentro de la plataforma,
/// opcionalmente asociado a una empresa concreta. El rol no pertenece
/// directamente a <c>Usuario</c> porque un mismo usuario puede tener
/// múltiples <c>UsuarioRol</c> simultáneamente.
/// No decide ni valida si una asignación, revocación o autoasignación de rol
/// está permitida: esas reglas de autorización corresponden a la capa de
/// aplicación, no a esta entidad.
/// </summary>
public class UsuarioRol
{
    /// <summary>Identificador único del registro de rol.</summary>
    public int UsuarioRolId { get; set; }

    /// <summary>Identificador del <c>Usuario</c> al que pertenece este rol.</summary>
    public int UsuarioId { get; set; }

    /// <summary>Rol asignado al usuario.</summary>
    public Rol Rol { get; set; }

    /// <summary>
    /// Empresa asociada a este rol. Obligatorio para <c>COORDINADOR</c>. Es
    /// <c>null</c> para <c>ADMINISTRADOR_PLATAFORMA</c> (rol global) y para
    /// <c>CONDUCTOR</c>, cuya relación con cada empresa se resuelve mediante
    /// <c>VinculacionConductorEmpresa</c> y no mediante este campo. Para
    /// <c>EMPLEADO</c> es <c>null</c> desde el autorregistro hasta que una
    /// importación de Excel encuentre su cédula y le asigne una empresa (ver
    /// <c>AGENTS.md</c> §15); mientras tanto, la persona puede iniciar sesión
    /// pero no tiene ninguna operación de transporte que consultar.
    /// </summary>
    public int? EmpresaId { get; set; }

    /// <summary>
    /// Indica si este rol está actualmente activo. Permite revocar un rol
    /// sin eliminar el registro histórico de la asignación.
    /// </summary>
    public bool Activo { get; set; }

    /// <summary>Usuario al que pertenece este rol.</summary>
    public Usuario? Usuario { get; set; }

    /// <summary>Empresa asociada a este rol, cuando aplica.</summary>
    public Empresa? Empresa { get; set; }
}
