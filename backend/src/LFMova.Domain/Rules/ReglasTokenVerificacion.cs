using LFMova.Domain.Entities;

namespace LFMova.Domain.Rules;

/// <summary>
/// Reglas de dominio sobre la validez de un <c>TokenVerificacion</c>. No
/// acceden a persistencia ni deciden qué hacer si la regla se incumple.
/// </summary>
public static class ReglasTokenVerificacion
{
    /// <summary>Indica si el token sigue siendo válido: no fue utilizado y no expiró.</summary>
    public static bool EsValido(TokenVerificacion token, DateTime ahora)
        => !token.Utilizado && token.FechaExpiracion > ahora;
}
