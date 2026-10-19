namespace LFMova.Application.Validators;

/// <summary>
/// Valida el formato básico de la dirección de una empresa. El SDD no define
/// un formato exacto, por lo que este validador se limita a garantizar que
/// no esté vacía.
/// </summary>
public static class DireccionValidador
{
    /// <summary>Indica si la dirección tiene un formato básico válido (no vacía).</summary>
    public static bool EsValida(string direccion) => !string.IsNullOrWhiteSpace(direccion);
}
