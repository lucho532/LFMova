using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.Domain.Rules;

/// <summary>
/// Reglas de dominio que garantizan la consistencia multiempresa entre
/// entidades relacionadas. Sus métodos reciben las entidades ya cargadas por
/// quien las invoca y evalúan si sus relaciones de empresa son consistentes.
/// No acceden a persistencia ni deciden qué hacer si la regla se incumple:
/// esa decisión (rechazar la operación, informar el error, etc.) corresponde
/// a la capa de aplicación. No debe considerarse el único mecanismo de
/// seguridad de la plataforma.
/// </summary>
public static class ReglasMultiempresa
{
    /// <summary>Indica si un empleado pertenece a la empresa indicada.</summary>
    public static bool EmpleadoPerteneceAEmpresa(Empleado empleado, int empresaId)
        => empleado.EmpresaId == empresaId;

    /// <summary>Indica si una sede pertenece a la empresa indicada.</summary>
    public static bool SedePerteneceAEmpresa(Sede sede, int empresaId)
        => sede.EmpresaId == empresaId;

    /// <summary>Indica si una zona pertenece a la empresa indicada.</summary>
    public static bool ZonaPerteneceAEmpresa(Zona zona, int empresaId)
        => zona.EmpresaId == empresaId;

    /// <summary>Indica si una macrozona pertenece a la empresa indicada.</summary>
    public static bool MacroZonaPerteneceAEmpresa(MacroZona macroZona, int empresaId)
        => macroZona.EmpresaId == empresaId;

    /// <summary>Indica si una barrera geográfica pertenece a la empresa indicada.</summary>
    public static bool BarreraGeograficaPerteneceAEmpresa(BarreraGeografica barrera, int empresaId)
        => barrera.EmpresaId == empresaId;

    /// <summary>Indica si un corredor vial pertenece a la empresa indicada.</summary>
    public static bool CorredorVialPerteneceAEmpresa(CorredorVial corredor, int empresaId)
        => corredor.EmpresaId == empresaId;

    /// <summary>Indica si una programación de transporte pertenece a la empresa indicada.</summary>
    public static bool ProgramacionPerteneceAEmpresa(ProgramacionTransporte programacion, int empresaId)
        => programacion.EmpresaId == empresaId;

    /// <summary>Indica si una jornada pertenece a la empresa indicada.</summary>
    public static bool JornadaPerteneceAEmpresa(Jornada jornada, int empresaId)
        => jornada.EmpresaId == empresaId;

    /// <summary>
    /// Indica si un servicio es consistente con una empresa: tanto su
    /// jornada como su sede deben pertenecer a esa misma empresa.
    /// </summary>
    public static bool ServicioEsConsistenteConEmpresa(Jornada jornada, Sede sede, int empresaId)
        => JornadaPerteneceAEmpresa(jornada, empresaId) && SedePerteneceAEmpresa(sede, empresaId);
}
