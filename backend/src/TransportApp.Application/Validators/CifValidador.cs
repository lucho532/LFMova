namespace TransportApp.Application.Validators;

/// <summary>
/// Valida el formato básico del CIF de una empresa. El SDD no define un
/// formato exacto, por lo que este validador se limita a garantizar que no
/// esté vacío; no inventa restricciones adicionales no definidas. La
/// unicidad se valida por separado contra la persistencia.
/// </summary>
public static class CifValidador
{
    /// <summary>Indica si el CIF tiene un formato básico válido (no vacío).</summary>
    public static bool EsValido(string cif) => !string.IsNullOrWhiteSpace(cif);
}
