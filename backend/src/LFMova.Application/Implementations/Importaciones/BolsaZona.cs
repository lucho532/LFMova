namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Bolsa de pendientes que se reparte junta en una misma ruta: empieza
/// siendo una zona/corredor, pero puede ser el resultado de fusionar
/// varias zonas pequeñas y cercanas (ver <see cref="RepartidorPorZona"/>).
/// Solo agrupa: no decide qué unidad la lleva.
/// </summary>
public sealed class BolsaZona
{
    /// <summary>Crea la bolsa de una zona con sus pendientes y las macrozonas de sus barrios.</summary>
    public BolsaZona(string clave, List<Pendiente> pendientes, HashSet<string> macroZonas)
    {
        Claves.Add(clave);
        Pendientes.AddRange(pendientes);
        MacroZonas = macroZonas;
    }

    /// <summary>Zonas o corredores que forman la bolsa (más de uno si ya se fusionó).</summary>
    public List<string> Claves { get; } = new();

    /// <summary>Pendientes que se reparten juntos.</summary>
    public List<Pendiente> Pendientes { get; } = new();

    /// <summary>
    /// Macrozonas de TODOS los barrios de la bolsa (no solo la primera): una bolsa ya fusionada
    /// por un corredor vial puede abarcar zonas de macrozonas distintas (un corredor real puede
    /// cruzar el límite administrativo de una comuna), así que dos bolsas se consideran cercanas
    /// si comparten AL MENOS una macrozona, no solo si coincide "la" macrozona de cada una.
    /// </summary>
    public HashSet<string> MacroZonas { get; }

    /// <summary>Indica que es la bolsa de las personas cuyo barrio no está en ninguna zona.</summary>
    public bool EsSinZona => Claves.Contains(MapaZonas.SinZonaAsignada);

    /// <summary>Nombre de la bolsa para los avisos al coordinador.</summary>
    public string Etiqueta => string.Join(" + ", Claves);
}
