using LFMova.Domain.Entities;

namespace LFMova.Domain.Rules;

/// <summary>
/// Reglas de dominio sobre el estado de una <see cref="InvitacionEmpresa"/>.
/// No acceden a persistencia ni deciden qué hacer si la regla se incumple.
/// </summary>
public static class ReglasInvitacionEmpresa
{
    /// <summary>Estado de una invitación que todavía puede aceptarse.</summary>
    public const string Pendiente = "PENDIENTE";

    /// <summary>Estado de una invitación que la persona ya aceptó.</summary>
    public const string Aceptada = "ACEPTADA";

    /// <summary>Estado de una invitación que venció sin aceptarse.</summary>
    public const string Vencida = "VENCIDA";

    /// <summary>Indica si la invitación todavía puede aceptarse: no fue aceptada y no venció.</summary>
    public static bool PuedeAceptarse(InvitacionEmpresa invitacion, DateTime ahora)
        => invitacion.FechaAceptacion is null && invitacion.FechaExpiracion > ahora;

    /// <summary>Estado derivado de la invitación en el momento indicado.</summary>
    public static string Estado(InvitacionEmpresa invitacion, DateTime ahora)
        => invitacion.FechaAceptacion is not null ? Aceptada
            : invitacion.FechaExpiracion > ahora ? Pendiente
            : Vencida;
}
