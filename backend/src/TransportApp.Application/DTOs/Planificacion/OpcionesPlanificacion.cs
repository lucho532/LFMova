namespace TransportApp.Application.DTOs.Planificacion;

/// <summary>
/// Configuración externa de la planificación asistida (sección de
/// configuración <c>"Planificacion"</c>). El valor de la ventana de recogida
/// es una referencia inicial de implementación, no una regla de negocio
/// cerrada (ver <c>tasks.md</c> T080): debe permanecer configurable y nunca
/// codificarse como constante fija dentro del algoritmo.
/// </summary>
public class OpcionesPlanificacion
{
    /// <summary>Nombre de la sección de configuración correspondiente.</summary>
    public const string Seccion = "Planificacion";

    /// <summary>
    /// Minutos de la ventana conjunta aproximada de recogida de todos los
    /// pasajeros de un servicio. Referencia inicial: 25 minutos, pendiente de
    /// validación (ver <c>spec.md</c> §28 y <c>data-model.md</c> §30).
    /// </summary>
    public int VentanaRecogidaMinutos { get; set; } = 25;
}
