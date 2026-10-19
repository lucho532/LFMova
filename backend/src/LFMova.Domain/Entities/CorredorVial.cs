namespace LFMova.Domain.Entities;

/// <summary>
/// Agrupa varias <see cref="Zona"/> que están sobre el mismo camino real: un
/// vehículo que va hacia una de ellas de todas formas pasa por las demás
/// (por ejemplo, para llegar a Morrogacho hay que pasar por La Francia). A
/// diferencia de <see cref="MacroZona"/> (que es solo organizativa), un
/// CorredorVial SÍ participa en el reparto: el algoritmo trata a todas las
/// Zonas de un mismo corredor como una sola bolsa de pasajeros antes de
/// repartir por capacidad, así que pueden terminar compartiendo un mismo
/// vehículo en vez de exigir uno por Zona. Nunca debe agrupar Zonas cuyos
/// barrios tengan una <see cref="BarreraGeografica"/> declarada entre sí.
/// </summary>
public class CorredorVial
{
    /// <summary>Identificador único del corredor vial.</summary>
    public int CorredorVialId { get; set; }

    /// <summary>Identificador de la empresa a la que pertenece este corredor.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Nombre del corredor, definido por el coordinador (por ejemplo "Atardeceres Occidente").</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Indica si el corredor está activo. Un corredor inactivo deja de
    /// agrupar a sus Zonas para el reparto (cada una vuelve a exigir su
    /// propio conductor), pero conserva su información histórica.
    /// </summary>
    public bool Activo { get; set; }

    /// <summary>Empresa a la que pertenece este corredor.</summary>
    public Empresa? Empresa { get; set; }

    /// <summary>Zonas agrupadas bajo este corredor.</summary>
    public ICollection<Zona> Zonas { get; set; } = new List<Zona>();
}
