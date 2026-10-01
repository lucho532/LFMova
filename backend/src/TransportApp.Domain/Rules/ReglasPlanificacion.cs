using TransportApp.Domain.Entities;

namespace TransportApp.Domain.Rules;

/// <summary>
/// Reglas de apoyo para la planificación asistida de rutas (ver
/// <c>spec.md</c> §28, <c>data-model.md</c> §30 y <c>tasks.md</c> Fase 14).
/// Calcula magnitudes objetivas (distancia, capacidad, orden heurístico,
/// ventanas horarias) a partir de datos reales. No decide la asignación
/// definitiva ni sustituye la decisión del coordinador, ni accede a
/// persistencia.
/// </summary>
public static class ReglasPlanificacion
{
    private const double RadioTierraKm = 6371.0;

    /// <summary>
    /// Calcula la distancia en línea recta entre dos coordenadas (fórmula de
    /// Haversine), en kilómetros. Devuelve <c>null</c> si alguna coordenada
    /// no está disponible. Es una aproximación geométrica, no una distancia
    /// de ruta real: no existe todavía un proveedor de mapas/rutas definido
    /// (ver <c>spec.md</c>, ítems pendientes de planificación).
    /// </summary>
    public static double? CalcularDistanciaKm(double? latitud1, double? longitud1, double? latitud2, double? longitud2)
    {
        if (latitud1 is null || longitud1 is null || latitud2 is null || longitud2 is null)
        {
            return null;
        }

        var dLat = GradosARadianes(latitud2.Value - latitud1.Value);
        var dLon = GradosARadianes(longitud2.Value - longitud1.Value);

        var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2))
                + (Math.Cos(GradosARadianes(latitud1.Value)) * Math.Cos(GradosARadianes(latitud2.Value))
                   * Math.Sin(dLon / 2) * Math.Sin(dLon / 2));

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return RadioTierraKm * c;
    }

    /// <summary>
    /// Indica si la cantidad de pasajeros de un servicio excede la capacidad
    /// del vehículo de la unidad candidata (ver <c>tasks.md</c> T077).
    /// </summary>
    public static bool ExcedeCapacidad(int cantidadPasajeros, int capacidadVehiculo)
        => cantidadPasajeros > capacidadVehiculo;

    /// <summary>
    /// Ordena los pasajeros de un servicio de <c>ENTRADA</c> según la
    /// heurística general de <c>spec.md</c> §28: el pasajero más alejado de
    /// la sede se recoge primero, avanzando progresivamente hacia pasajeros
    /// más cercanos hasta llegar a la sede. Es una heurística general, no una
    /// regla absoluta (ver <c>tasks.md</c> T079): el coordinador puede
    /// modificar el orden propuesto. Los pasajeros sin coordenadas conocidas
    /// se ubican al final, en el orden en que fueron recibidos, porque no
    /// pueden ubicarse geográficamente.
    /// </summary>
    public static List<ServicioPasajero> OrdenarParaEntrada(
        IEnumerable<ServicioPasajero> pasajeros,
        double? sedeLatitud,
        double? sedeLongitud)
    {
        return pasajeros
            .Select((pasajero, indice) => (
                pasajero,
                distancia: CalcularDistanciaKm(pasajero.Latitud, pasajero.Longitud, sedeLatitud, sedeLongitud),
                indice))
            .OrderBy(x => x.distancia is null)
            .ThenByDescending(x => x.distancia ?? 0)
            .ThenBy(x => x.indice)
            .Select(x => x.pasajero)
            .ToList();
    }

    /// <summary>
    /// Calcula la hora límite de llegada a la sede para un servicio de
    /// <c>ENTRADA</c>: al menos <paramref name="minutosAnticipacion"/>
    /// minutos antes de la hora programada (ver <c>spec.md</c> §28, valor
    /// definido: 15 minutos).
    /// </summary>
    public static TimeOnly HoraLimiteLlegadaSede(TimeOnly horaProgramada, int minutosAnticipacion = 15)
        => horaProgramada.AddMinutes(-minutosAnticipacion);

    /// <summary>
    /// Calcula la hora sugerida de inicio de recogida restando la ventana
    /// conjunta aproximada de recogida (parámetro configurable, ver
    /// <c>tasks.md</c> T080) a la hora límite de llegada a la sede. No modela
    /// el tiempo de desplazamiento del último punto de recogida hacia la
    /// sede: no existe todavía un proveedor de cálculo de rutas/tiempos
    /// definido, por lo que esta sugerencia es aproximada.
    /// </summary>
    public static TimeOnly HoraSugeridaInicioRecogida(TimeOnly horaLimiteLlegadaSede, int ventanaRecogidaMinutos)
        => horaLimiteLlegadaSede.AddMinutes(-ventanaRecogidaMinutos);

    private static double GradosARadianes(double grados) => grados * Math.PI / 180.0;
}
