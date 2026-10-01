using TransportApp.Domain.Entities;

namespace TransportApp.Domain.Rules;

/// <summary>
/// Reglas de dominio que garantizan la consistencia de un
/// <c>ServicioPasajero</c> respecto al <c>Servicio</c>, la
/// <c>ProgramacionTransporte</c> y el <c>Empleado</c> relacionados. No
/// acceden a persistencia; reciben las entidades ya cargadas por quien las
/// invoca.
/// </summary>
public static class ReglasServicioPasajero
{
    /// <summary>Indica si el pasajero pertenece efectivamente al servicio indicado.</summary>
    public static bool PerteneceAlServicio(ServicioPasajero servicioPasajero, Servicio servicio)
        => servicioPasajero.ServicioId == servicio.ServicioId;

    /// <summary>Indica si la programación pertenece al mismo contexto empresarial indicado.</summary>
    public static bool ProgramacionEsDelMismoContextoEmpresarial(ProgramacionTransporte programacion, int empresaId)
        => programacion.EmpresaId == empresaId;

    /// <summary>Indica si el empleado del pasajero corresponde al empleado de la programación.</summary>
    public static bool EmpleadoCorrespondeAProgramacion(ServicioPasajero servicioPasajero, ProgramacionTransporte programacion)
        => servicioPasajero.EmpleadoId == programacion.EmpleadoId;

    /// <summary>
    /// Indica si la programación indicada ya está asignada a un
    /// <c>ServicioPasajero</c> existente (una programación no puede estar
    /// asignada a más de uno).
    /// </summary>
    public static bool ProgramacionYaAsignada(
        int programacionTransporteId,
        IEnumerable<ServicioPasajero> servicioPasajerosExistentes)
        => servicioPasajerosExistentes.Any(sp => sp.ProgramacionTransporteId == programacionTransporteId);

    /// <summary>
    /// Indica si la sede de la programación coincide con la sede del
    /// servicio al que se asignaría como <c>ServicioPasajero</c>. Garantiza
    /// que un <c>Servicio</c> nunca contenga pasajeros de sedes distintas: la
    /// sede es un atributo del propio <c>Servicio</c>, no de cada pasajero.
    /// </summary>
    public static bool ProgramacionCoincideConSedeDelServicio(ProgramacionTransporte programacion, Servicio servicio)
        => programacion.SedeId == servicio.SedeId;
}
