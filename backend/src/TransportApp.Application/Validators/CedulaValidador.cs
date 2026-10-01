namespace TransportApp.Application.Validators;

/// <summary>
/// Valida el formato básico de una cédula. El SDD no define un formato
/// exacto (longitud, solo dígitos, etc. quedan sin especificar), por lo que
/// este validador se limita a garantizar que no esté vacía; no inventa
/// restricciones adicionales no definidas.
/// </summary>
public static class CedulaValidador
{
    /// <summary>Indica si la cédula tiene un formato básico válido (no vacía).</summary>
    public static bool EsValida(string cedula) => !string.IsNullOrWhiteSpace(cedula);
}
