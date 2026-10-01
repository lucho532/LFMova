namespace TransportApp.Domain.Entities;

/// <summary>
/// Agrupa varias <see cref="Zona"/> (subzonas) bajo un nombre de comuna o
/// sector amplio de la ciudad (por ejemplo "Atardeceres"), únicamente para
/// que el coordinador organice y encuentre sus zonas en pantalla. No
/// participa en el reparto de rutas: cada <see cref="Zona"/> sigue exigiendo
/// su propio conductor por separado, sin importar si comparte MacroZona con
/// otra.
/// </summary>
public class MacroZona
{
    /// <summary>Identificador único de la macrozona.</summary>
    public int MacroZonaId { get; set; }

    /// <summary>Identificador de la empresa a la que pertenece esta macrozona.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Nombre de la macrozona, definido por el coordinador (por ejemplo "Atardeceres").</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Indica si la macrozona está activa. Una macrozona inactiva conserva
    /// su información histórica y no se elimina físicamente, pero no se
    /// ofrece para clasificar nuevas zonas.
    /// </summary>
    public bool Activa { get; set; }

    /// <summary>Empresa a la que pertenece esta macrozona.</summary>
    public Empresa? Empresa { get; set; }

    /// <summary>Zonas (subzonas) agrupadas bajo esta macrozona.</summary>
    public ICollection<Zona> Zonas { get; set; } = new List<Zona>();
}
