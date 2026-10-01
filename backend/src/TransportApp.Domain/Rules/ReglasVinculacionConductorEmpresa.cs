using TransportApp.Domain.Entities;

namespace TransportApp.Domain.Rules;

/// <summary>
/// Regla de dominio sobre la vinculación entre un <c>Conductor</c> y una
/// <c>Empresa</c>. No accede a persistencia ni decide qué hacer si la regla
/// se incumple.
/// </summary>
public static class ReglasVinculacionConductorEmpresa
{
    /// <summary>
    /// Indica si el conductor ya tiene una vinculación (activa o inactiva)
    /// con la empresa indicada, dado el conjunto de vinculaciones que ya
    /// tiene, para evitar registros duplicados.
    /// </summary>
    public static bool YaExisteVinculacion(
        IEnumerable<VinculacionConductorEmpresa> vinculacionesDelConductor,
        int empresaId)
        => vinculacionesDelConductor.Any(v => v.EmpresaId == empresaId);
}
