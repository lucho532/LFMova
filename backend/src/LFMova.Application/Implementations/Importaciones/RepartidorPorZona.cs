namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Decide, para una ruta (misma sede, tipo, fecha y hora), qué pendientes lleva cada unidad libre,
/// agrupándolos por zona. Es un cálculo puro: no consulta la base de datos ni crea servicios, y nunca
/// usa barrios codificados, solo las zonas que el propio coordinador definió.
/// </summary>
public static class RepartidorPorZona
{
    /// <summary>Mínimo de pasajeros que debe llevar una ruta nueva antes de abrirla, para no enviar un carro por 1-2-3 personas cuando otra zona vecina puede compartirlo.</summary>
    private const int MinimoPasajerosPorRuta = 4;

    /// <summary>
    /// Decide qué pendientes lleva cada unidad para una ruta (misma sede,
    /// tipo, fecha y hora). Primero agrupa a los pendientes por zona (según
    /// el barrio de cada uno): una misma ruta puede necesitar varios
    /// conductores en paralelo, uno por zona, porque un solo carro no
    /// alcanza a cubrir toda la ciudad en el tiempo del servicio (ver
    /// AGENTS.md sobre continuidad geográfica; nunca se codifican barrios
    /// fijos, se usa la lista de zonas que el propio coordinador definió).
    /// Antes de asignar unidades, fusiona zonas con menos de
    /// <see cref="MinimoPasajerosPorRuta"/> pendientes con otra zona de la
    /// misma macrozona (ver <see cref="FusionarZonasPequenas"/>): no vale la
    /// pena enviar un carro por 1-2-3 personas si hay otra zona vecina que
    /// puede compartir el vehículo.
    /// Dentro de cada bolsa ya fusionada, usa las unidades libres a esa hora,
    /// las de mayor capacidad primero y solo las necesarias, repartiendo de
    /// forma pareja sin superar la capacidad de ninguna. Si a alguna bolsa no
    /// le alcanzan las unidades libres, no bloquea la importación: dejando
    /// esos pendientes agrupados en una última entrada con unidad nula
    /// (servicio sin conductor asignado), para que el coordinador los
    /// reasigne a mano.
    /// </summary>
    public static List<(int? Unidad, List<Pendiente> Filas)> Repartir(
        List<UnidadDisponible> unidades, HashSet<int> usadas, List<Pendiente> pendientes,
        MapaZonas mapaZonas, string titulo, TimeOnly hora, DateOnly fecha, List<string> advertencias)
    {
        var gruposPorZona = pendientes.GroupBy(p => mapaZonas.ResolverZona(p.Fila.Barrio, p.Fila.Direccion)).ToList();

        var barriosSinZona = gruposPorZona.FirstOrDefault(g => g.Key == MapaZonas.SinZonaAsignada)?.Select(p => p.Fila.Barrio).Distinct().ToList();
        if (barriosSinZona is { Count: > 0 })
        {
            advertencias.Add(
                $"Estos barrios de {titulo} ({hora:HH\\:mm}) no están asignados a ninguna zona, así que se reparten aparte: {string.Join(", ", barriosSinZona)}.");
        }

        var bolsas = FusionarZonasPequenas(gruposPorZona, mapaZonas, advertencias, titulo, hora);

        var resultado = new List<(int?, List<Pendiente>)>();
        var sinAsignar = new List<Pendiente>();
        foreach (var bolsa in bolsas.OrderByDescending(b => b.Pendientes.Count))
        {
            var pendientesDeLaZona = bolsa.Pendientes;
            var libres = unidades.Where(u => !usadas.Contains(u.UnidadOperativaId)).OrderByDescending(u => u.Capacidad).ToList();

            var elegidas = new List<UnidadDisponible>();
            var capacidad = 0;
            foreach (var unidad in libres)
            {
                if (capacidad >= pendientesDeLaZona.Count)
                {
                    break;
                }

                elegidas.Add(unidad);
                capacidad += unidad.Capacidad;
            }

            var aAsignar = pendientesDeLaZona;
            if (capacidad < pendientesDeLaZona.Count)
            {
                var nombreZona = bolsa.EsSinZona ? "sin zona asignada" : $"la zona \"{bolsa.Etiqueta}\"";
                var faltan = pendientesDeLaZona.Count - capacidad;
                advertencias.Add(
                    $"No hay unidades suficientes para {faltan} pasajero(s) de {nombreZona} en {titulo} ({hora:HH\\:mm}, {fecha:yyyy-MM-dd}): "
                    + $"la capacidad libre a esa hora es {capacidad}. Quedan sin conductor asignado al final de la lista de este horario para reasignarlos manualmente.");
                aAsignar = pendientesDeLaZona.Take(capacidad).ToList();
                sinAsignar.AddRange(pendientesDeLaZona.Skip(capacidad));
            }

            var restantes = aAsignar.Count;
            var porAsignar = elegidas.OrderBy(u => u.Capacidad).ToList();
            var indice = 0;
            for (var i = 0; i < porAsignar.Count; i++)
            {
                var cantidad = Math.Min(porAsignar[i].Capacidad, (int)Math.Ceiling(restantes / (double)(porAsignar.Count - i)));
                if (cantidad <= 0)
                {
                    continue;
                }

                resultado.Add((porAsignar[i].UnidadOperativaId, aAsignar.Skip(indice).Take(cantidad).ToList()));
                usadas.Add(porAsignar[i].UnidadOperativaId);
                indice += cantidad;
                restantes -= cantidad;
            }
        }

        if (sinAsignar.Count > 0)
        {
            resultado.Add((null, sinAsignar));
        }

        return resultado;
    }

    /// <summary>
    /// Fusiona zonas con menos de <see cref="MinimoPasajerosPorRuta"/>
    /// pendientes con otra zona de la MISMA macrozona (comuna/sector amplio
    /// que el coordinador ya declaró), hasta alcanzar el mínimo o hasta
    /// quedarse sin otra zona vecina para juntar. La cercanía nunca se
    /// inventa por coordenadas o distancia calculada (no hay ese dato): se
    /// usa únicamente la macrozona que el propio coordinador asignó a cada
    /// zona, así que dos zonas sin macrozona (o de macrozonas distintas)
    /// nunca se fusionan entre sí. El balde "sin zona asignada" tampoco se
    /// fusiona nunca: no hay ningún dato de cercanía para decidir con qué
    /// otra zona juntarlo.
    /// </summary>
    private static List<BolsaZona> FusionarZonasPequenas(
        List<IGrouping<string, Pendiente>> gruposPorZona, MapaZonas mapaZonas,
        List<string> advertencias, string titulo, TimeOnly hora)
    {
        var bolsas = gruposPorZona
            .Where(g => g.Key != MapaZonas.SinZonaAsignada)
            .Select(g => new BolsaZona(g.Key, g.ToList(), ResolverMacroZonas(g, mapaZonas)))
            .ToList();

        var sinFusionPosible = new HashSet<BolsaZona>();
        while (true)
        {
            var pequena = bolsas.FirstOrDefault(b => b.Pendientes.Count < MinimoPasajerosPorRuta && !sinFusionPosible.Contains(b));
            if (pequena is null)
            {
                break;
            }

            // Solo se fusiona con otra bolsa que TODAVÍA esté por debajo del mínimo: una bolsa que ya
            // alcanzó el mínimo deja de ser candidata, para que no seguir absorbiendo zonas indefinidamente
            // (dos zonas pueden compartir macrozona con una tercera sin ser cercanas entre sí; sin este
            // límite, una bolsa ya fusionada podía seguir creciendo y terminar mezclando comunas sin
            // relación real entre sí con tal de que cada fusión, vista de a una, compartiera una macrozona).
            var vecina = bolsas
                .Where(b => b != pequena && b.Pendientes.Count < MinimoPasajerosPorRuta && b.MacroZonas.Overlaps(pequena.MacroZonas))
                .OrderBy(b => b.Pendientes.Count)
                .FirstOrDefault();

            if (vecina is null)
            {
                sinFusionPosible.Add(pequena);
                advertencias.Add(
                    $"La zona \"{pequena.Etiqueta}\" de {titulo} ({hora:HH\\:mm}) tiene menos de {MinimoPasajerosPorRuta} pasajeros y no hay otra zona de la misma "
                    + "macrozona para juntarla en una sola ruta: se reparte igual, sola.");
                continue;
            }

            vecina.Claves.AddRange(pequena.Claves);
            vecina.Pendientes.AddRange(pequena.Pendientes);
            vecina.MacroZonas.UnionWith(pequena.MacroZonas);
            bolsas.Remove(pequena);
            sinFusionPosible.Remove(vecina);
        }

        var sinZona = gruposPorZona.FirstOrDefault(g => g.Key == MapaZonas.SinZonaAsignada);
        if (sinZona is not null)
        {
            bolsas.Add(new BolsaZona(sinZona.Key, sinZona.ToList(), new HashSet<string>()));
        }

        return bolsas;
    }

    /// <summary>Macrozonas de todos los barrios de una bolsa (una bolsa ya fusionada por corredor vial puede tener barrios de más de una macrozona).</summary>
    private static HashSet<string> ResolverMacroZonas(IEnumerable<Pendiente> pendientes, MapaZonas mapaZonas)
    {
        var resultado = new HashSet<string>();
        foreach (var pendiente in pendientes)
        {
            if (mapaZonas.ResolverZonaCandidata(pendiente.Fila.Barrio, pendiente.Fila.Direccion)?.MacroZona is { } macroZona)
            {
                resultado.Add(macroZona);
            }
        }

        return resultado;
    }
}
