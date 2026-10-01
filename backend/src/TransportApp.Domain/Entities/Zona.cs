namespace TransportApp.Domain.Entities;

/// <summary>
/// Representa una zona geográfica de recogida definida por el coordinador de
/// una empresa (por ejemplo "Zona Norte"), como agrupación de barrios. Su
/// responsabilidad es permitir clasificar a cada empleado por cercanía real
/// dentro de la ciudad, para que el reparto automático de una ruta asigne un
/// conductor por zona en vez de solo por capacidad del vehículo (una misma
/// hora y sede puede necesitar varios conductores en paralelo cuando la
/// ciudad es grande).
/// No decide por sí misma el reparto ni valida su pertenencia a una empresa;
/// tampoco calcula distancias ni coordenadas: la cercanía se define aquí
/// únicamente por la lista de barrios que el coordinador le asigna.
/// </summary>
public class Zona
{
    /// <summary>Identificador único de la zona.</summary>
    public int ZonaId { get; set; }

    /// <summary>Identificador de la empresa a la que pertenece esta zona.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Nombre de la zona, definido por el coordinador (por ejemplo "Zona Norte").</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Barrios que el coordinador considera parte de esta zona. Un mismo
    /// barrio no debería repetirse en más de una zona activa de la empresa,
    /// pero esta entidad no impone esa restricción por sí sola.
    /// </summary>
    public List<string> Barrios { get; set; } = new();

    /// <summary>
    /// Indica si la zona está activa. Una zona inactiva conserva su
    /// información histórica y no se elimina físicamente, pero no se ofrece
    /// para nuevo reparto.
    /// </summary>
    public bool Activa { get; set; }

    /// <summary>
    /// Macrozona (comuna o sector amplio) bajo la que se organiza esta zona
    /// en pantalla, si el coordinador la clasificó. Es opcional y no afecta
    /// el reparto: dos zonas de la misma macrozona igual exigen conductores
    /// distintos.
    /// </summary>
    public int? MacroZonaId { get; set; }

    /// <summary>
    /// Corredor vial al que pertenece esta zona, si el coordinador la
    /// clasificó. A diferencia de <see cref="MacroZonaId"/>, esto SÍ afecta
    /// el reparto: las zonas de un mismo corredor se tratan como una sola
    /// bolsa de pasajeros y pueden compartir vehículo (ver <see cref="CorredorVial"/>).
    /// </summary>
    public int? CorredorVialId { get; set; }

    /// <summary>Empresa a la que pertenece esta zona.</summary>
    public Empresa? Empresa { get; set; }

    /// <summary>Macrozona bajo la que se organiza esta zona, si tiene una asignada.</summary>
    public MacroZona? MacroZona { get; set; }

    /// <summary>Corredor vial al que pertenece esta zona, si tiene uno asignado.</summary>
    public CorredorVial? CorredorVial { get; set; }
}
