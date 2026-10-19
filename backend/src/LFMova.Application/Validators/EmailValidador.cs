namespace LFMova.Application.Validators;

/// <summary>
/// Valida el formato básico de un correo electrónico. El SDD no define un
/// formato exacto más allá de exigir que contenga un "@" con contenido a
/// ambos lados; no inventa restricciones adicionales no definidas.
/// </summary>
public static class EmailValidador
{
    /// <summary>Indica si el correo tiene un formato básico válido.</summary>
    public static bool EsValido(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var arroba = email.IndexOf('@');
        return arroba > 0 && arroba < email.Length - 1 && !email.Contains(' ');
    }
}
