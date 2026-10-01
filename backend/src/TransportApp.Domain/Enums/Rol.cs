namespace TransportApp.Domain.Enums;

/// <summary>
/// Representa los roles funcionales que un <c>Usuario</c> puede tener dentro de
/// la plataforma. Los roles se asignan mediante la entidad <c>UsuarioRol</c> y
/// un mismo usuario puede tener varios simultáneamente.
/// No representa una identidad ni sustituye a <c>Usuario</c>.
/// </summary>
public enum Rol
{
    /// <summary>
    /// Responsable global de la plataforma. Crea empresas y asigna su primer
    /// coordinador. No participa en la operación diaria de las empresas.
    /// </summary>
    ADMINISTRADOR_PLATAFORMA,

    /// <summary>
    /// Administra la operación de una única empresa: empleados, conductores,
    /// vehículos, sedes, programaciones, jornadas, servicios y publicaciones.
    /// </summary>
    COORDINADOR,

    /// <summary>
    /// Ejecuta servicios de transporte. Puede estar vinculado a múltiples
    /// empresas mediante <c>VinculacionConductorEmpresa</c>.
    /// </summary>
    CONDUCTOR,

    /// <summary>
    /// Consulta y gestiona su propia información de transporte dentro de la
    /// empresa a la que pertenece actualmente.
    /// </summary>
    EMPLEADO
}
