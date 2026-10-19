namespace LFMova.Application.Validators;

/// <summary>
/// Valida el formato básico de una contraseña. El SDD no define una
/// política de complejidad (longitud mínima, caracteres obligatorios,
/// etc.), por lo que este validador se limita a garantizar que no esté
/// vacía; no inventa restricciones adicionales no definidas.
/// </summary>
public static class PasswordValidador
{
    /// <summary>Indica si la contraseña tiene un formato básico válido (no vacía).</summary>
    public static bool EsValida(string password) => !string.IsNullOrWhiteSpace(password);
}
