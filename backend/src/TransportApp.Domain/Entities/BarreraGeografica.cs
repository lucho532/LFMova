namespace TransportApp.Domain.Entities;

/// <summary>
/// Declara que dos barrios de la empresa, aunque puedan estar geográficamente
/// cerca o pertenecer a la misma comuna, no tienen una conexión vial
/// aprovechable para el transporte (una barrera topográfica, una vía sin
/// continuidad, etc.). Es una relación simétrica: no importa el orden de
/// <see cref="BarrioA"/> y <see cref="BarrioB"/>.
/// Su único efecto es impedir que ambos barrios queden juntos dentro de una
/// misma <see cref="Zona"/>: no decide por sí misma el reparto ni calcula
/// rutas ni distancias.
/// </summary>
public class BarreraGeografica
{
    /// <summary>Identificador único de la barrera.</summary>
    public int BarreraGeograficaId { get; set; }

    /// <summary>Identificador de la empresa a la que pertenece esta barrera.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Uno de los dos barrios sin conexión aprovechable entre sí.</summary>
    public string BarrioA { get; set; } = string.Empty;

    /// <summary>El otro barrio sin conexión aprovechable con <see cref="BarrioA"/>.</summary>
    public string BarrioB { get; set; } = string.Empty;

    /// <summary>Motivo de la barrera, para que quede documentado (por ejemplo "barrera topográfica").</summary>
    public string? Motivo { get; set; }

    /// <summary>Empresa a la que pertenece esta barrera.</summary>
    public Empresa? Empresa { get; set; }
}
