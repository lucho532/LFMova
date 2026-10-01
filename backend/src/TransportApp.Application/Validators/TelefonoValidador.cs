namespace TransportApp.Application.Validators;

/// <summary>
/// Valida el formato básico de un teléfono de contacto. El SDD no define un
/// formato exacto (indicativo, longitud, etc.), por lo que este validador se
/// limita a garantizar que no esté vacío; no inventa restricciones
/// adicionales no definidas.
/// </summary>
public static class TelefonoValidador
{
    /// <summary>Indica si el teléfono tiene un formato básico válido (no vacío).</summary>
    public static bool EsValido(string telefono) => !string.IsNullOrWhiteSpace(telefono);
}
