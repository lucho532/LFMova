namespace LFMova.Application.DTOs.ServiciosPasajero;

/// <summary>
/// Datos con los que un empleado confirma su asistencia a un servicio.
/// Permite opcionalmente proporcionar una nueva dirección de recogida (ver
/// <c>tasks.md</c> T072A) y decidir si esa dirección pasa a ser su dirección
/// habitual.
/// </summary>
public class ConfirmarServicioPasajeroDto
{
    /// <summary>
    /// Nueva dirección de recogida para este servicio. Si es <c>null</c> o
    /// está vacía, se conserva la dirección ya registrada en el
    /// <c>ServicioPasajero</c>.
    /// </summary>
    public string? NuevaDireccion { get; set; }

    /// <summary>Nuevo barrio de recogida para este servicio, acompañando a <see cref="NuevaDireccion"/>.</summary>
    public string? NuevoBarrio { get; set; }

    /// <summary>Latitud de la nueva dirección, si está disponible.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud de la nueva dirección, si está disponible.</summary>
    public double? Longitud { get; set; }

    /// <summary>
    /// Indica si la nueva dirección debe establecerse como dirección
    /// habitual del empleado (<c>Empleado.Direccion</c>), conservando la
    /// anterior en <c>UbicacionRecogidaHistorica</c>. Sin efecto si
    /// <see cref="NuevaDireccion"/> no se proporciona.
    /// </summary>
    public bool EstablecerComoHabitual { get; set; }
}
