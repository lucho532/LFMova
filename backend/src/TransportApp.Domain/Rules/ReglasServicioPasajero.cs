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
    /// <summary>Indica si la programación pertenece al mismo contexto empresarial indicado.</summary>
    public static bool ProgramacionEsDelMismoContextoEmpresarial(ProgramacionTransporte programacion, int empresaId)
        => programacion.EmpresaId == empresaId;

    /// <summary>
    /// Indica si la sede de la programación coincide con la sede del
    /// servicio al que se asignaría como <c>ServicioPasajero</c>. Garantiza
    /// que un <c>Servicio</c> nunca contenga pasajeros de sedes distintas: la
    /// sede es un atributo del propio <c>Servicio</c>, no de cada pasajero.
    /// </summary>
    public static bool ProgramacionCoincideConSedeDelServicio(ProgramacionTransporte programacion, Servicio servicio)
        => programacion.SedeId == servicio.SedeId;
}
