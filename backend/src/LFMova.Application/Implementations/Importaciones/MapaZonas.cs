using LFMova.Application.Utils;
using LFMova.Domain.Entities;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Barrio (normalizado) → zonas que lo contienen, armado con las zonas activas que el coordinador
/// definió para la empresa. Un barrio puede estar en más de una zona cuando el mismo nombre existe en
/// dos ciudades; la elección la hace <see cref="ResolverZonaCandidata"/> mirando la dirección de la
/// persona. Solo clasifica barrios: no reparte pasajeros ni consulta la base de datos.
/// </summary>
public sealed class MapaZonas
{
    /// <summary>Grupo de reparto de las personas cuyo barrio no está en ninguna zona.</summary>
    public const string SinZonaAsignada = "Sin zona asignada";

    /// <summary>
    /// Marca un servicio existente cuyos pasajeros ya mezclan más de una zona (típicamente porque se
    /// armó antes de que existieran las Zonas, o antes de que este chequeo existiera). Nunca coincide
    /// con ninguna zona real ni con <see cref="SinZonaAsignada"/>, así que ese servicio deja de
    /// completarse automáticamente y las importaciones futuras arman una ruta nueva en su lugar.
    /// </summary>
    public const string ZonaMezclada = "__zona_mezclada__";

    private readonly Dictionary<string, List<ZonaCandidata>> _zonasPorBarrio;

    private MapaZonas(Dictionary<string, List<ZonaCandidata>> zonasPorBarrio)
    {
        _zonasPorBarrio = zonasPorBarrio;
    }

    /// <summary>Mapa sin ninguna zona: todos los barrios quedan sin clasificar.</summary>
    public static MapaZonas Vacio { get; } = new(new Dictionary<string, List<ZonaCandidata>>());

    /// <summary>
    /// Arma el mapa a partir de las zonas de la empresa (solo cuentan las activas). El grupo de reparto
    /// es normalmente el nombre de la zona, pero si la zona pertenece a un <see cref="CorredorVial"/>
    /// activo, el grupo es el nombre de ESE corredor: así, varias zonas sobre el mismo camino real (por
    /// ejemplo, para llegar a Morrogacho hay que pasar por La Francia) se tratan como una sola bolsa de
    /// pasajeros antes de repartir por capacidad, en vez de exigir un conductor por zona. La macrozona
    /// (comuna o sector amplio, ver <see cref="MacroZona"/>) se usa aparte, solo para decidir qué zonas
    /// pequeñas están lo bastante cerca como para fusionarse y no abrir una ruta de 1-2-3 pasajeros
    /// (ver <see cref="RepartidorPorZona"/>). Un barrio que no aparece en ninguna zona simplemente no
    /// tiene entrada en el mapa.
    /// </summary>
    public static MapaZonas Crear(IEnumerable<Zona> zonas)
    {
        var zonasPorBarrio = new Dictionary<string, List<ZonaCandidata>>();
        foreach (var zona in zonas.Where(z => z.Activa))
        {
            var grupo = zona.CorredorVial is { Activo: true } corredor ? corredor.Nombre : zona.Nombre;
            var macroZona = zona.MacroZona is { Activa: true } macro ? macro.Nombre : null;
            // Las zonas de la macrozona "Villamaría" son las de esa ciudad (mismo criterio que usa el frontend).
            var esVillamaria = zona.MacroZona is not null && TextoNormalizador.Normalizar(zona.MacroZona.Nombre) == "VILLAMARIA";
            foreach (var barrio in zona.Barrios)
            {
                var clave = TextoNormalizador.Normalizar(barrio);
                if (!zonasPorBarrio.TryGetValue(clave, out var candidatas))
                {
                    zonasPorBarrio[clave] = candidatas = new List<ZonaCandidata>();
                }

                candidatas.Add(new ZonaCandidata(grupo, macroZona, esVillamaria));
            }
        }

        return new MapaZonas(zonasPorBarrio);
    }

    /// <summary>Resuelve el grupo de reparto (zona o corredor) de una persona, o <see cref="SinZonaAsignada"/> si su barrio no está clasificado.</summary>
    public string ResolverZona(string barrio, string? direccion) =>
        ResolverZonaCandidata(barrio, direccion)?.Grupo ?? SinZonaAsignada;

    /// <summary>
    /// Elige, entre las zonas cuyo barrio coincide con el de la persona, la que le corresponde. Un mismo
    /// nombre de barrio puede existir en Villamaría y en Manizales (por ejemplo, "Nuevo Horizonte"): si la
    /// dirección pide un cruce de calle y carrera que no existe en Villamaría (ver
    /// <see cref="ReglasNomenclaturaVillamaria"/>), se descartan las zonas de la macrozona Villamaría; si
    /// la dirección sí es posible en Villamaría, se prefieren esas. Sin dirección reconocible se decide
    /// solo por el nombre del barrio, como siempre. Devuelve <c>null</c> si ninguna zona le sirve.
    /// </summary>
    public ZonaCandidata? ResolverZonaCandidata(string barrio, string? direccion)
    {
        if (!_zonasPorBarrio.TryGetValue(TextoNormalizador.Normalizar(barrio), out var candidatas))
        {
            return null;
        }

        return ReglasNomenclaturaVillamaria.DireccionPuedeSerDeVillamaria(direccion) switch
        {
            false => candidatas.LastOrDefault(c => !c.EsVillamaria),
            true => candidatas.LastOrDefault(c => c.EsVillamaria) ?? candidatas[^1],
            null => candidatas[^1],
        };
    }

    /// <summary>
    /// De las personas indicadas, los barrios que no coinciden con el de ninguna zona (texto exacto, sin
    /// mayúsculas ni tildes), en orden alfabético y sin repetidos. Sirve para que el coordinador note y
    /// corrija estos barrios antes de que el reparto automático los deje sin zona.
    /// </summary>
    public List<string> ObtenerBarriosSinZona(IEnumerable<(string Barrio, string Direccion)> personas) =>
        personas
            .Where(p => !string.IsNullOrWhiteSpace(p.Barrio))
            .Where(p => ResolverZonaCandidata(p.Barrio, p.Direccion) is null)
            .Select(p => p.Barrio.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(b => b, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
