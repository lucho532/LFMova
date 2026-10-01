using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;

namespace TransportApp.Domain.Rules;

/// <summary>
/// Regla de dominio que garantiza que una <c>UnidadOperativa</c> sea
/// consistente con el conductor propietario de su vehículo, y que su
/// asignación a servicios no genere conflictos temporales. No accede a
/// persistencia ni decide qué hacer si la regla se incumple.
/// </summary>
public static class ReglasUnidadOperativa
{
    /// <summary>
    /// Indica si la unidad operativa es consistente: debe referenciar el
    /// vehículo indicado y el conductor de la unidad debe coincidir con el
    /// conductor propietario de ese vehículo.
    /// </summary>
    public static bool EsConsistente(UnidadOperativa unidadOperativa, Vehiculo vehiculo)
        => unidadOperativa.VehiculoId == vehiculo.VehiculoId
           && unidadOperativa.ConductorId == vehiculo.ConductorId;

    /// <summary>
    /// Indica si asignar <paramref name="candidato"/> a una unidad operativa
    /// generaría un conflicto temporal con alguno de
    /// <paramref name="otrosServiciosDeLaUnidad"/>: una misma unidad no puede
    /// atender dos servicios con exactamente la misma fecha y hora
    /// programadas (imposibilidad física de estar en dos lugares a la vez),
    /// salvo la excepción de <see cref="SeCruzan"/>: una entrada y una salida
    /// de la misma sede a la misma hora sí son compatibles.
    /// Los servicios <c>CANCELADO</c> o <c>FINALIZADO</c> no cuentan como
    /// conflicto, ya que ya no representan una ocupación real de la unidad:
    /// uno nunca ocurrió y el otro ya terminó.
    /// </summary>
    public static bool HayConflictoTemporal(Servicio candidato, IEnumerable<Servicio> otrosServiciosDeLaUnidad)
        => otrosServiciosDeLaUnidad.Any(s =>
            s.ServicioId != candidato.ServicioId
            && s.Estado is not (EstadoServicio.CANCELADO or EstadoServicio.FINALIZADO)
            && SeCruzan(s.Fecha, s.HoraProgramada, s.Tipo, s.SedeId, candidato.Fecha, candidato.HoraProgramada, candidato.Tipo, candidato.SedeId));

    /// <summary>
    /// Indica si dos rutas no pueden ser atendidas por la misma unidad: tienen
    /// la misma fecha y hora programadas. La única excepción (decisión del
    /// usuario, 2026-09-30) es una ruta de entrada y otra de salida de la
    /// misma sede a la misma hora: el conductor llega a la sede con quienes
    /// entran y sale de ahí mismo con quienes terminan turno. Dos rutas del
    /// mismo tipo, o de sedes distintas, a la misma hora siguen cruzándose.
    /// No mira estados: quien la use decide qué servicios siguen ocupando a la unidad.
    /// </summary>
    public static bool SeCruzan(
        DateOnly fechaA, TimeOnly horaA, TipoServicio tipoA, int sedeIdA,
        DateOnly fechaB, TimeOnly horaB, TipoServicio tipoB, int sedeIdB)
        => fechaA == fechaB && horaA == horaB && !(tipoA != tipoB && sedeIdA == sedeIdB);
}
