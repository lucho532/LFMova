namespace TransportApp.Application.DTOs.Planificacion;

/// <summary>
/// Representa la posición propuesta de un pasajero dentro del orden sugerido
/// de un servicio (ver <c>tasks.md</c> T076/T081). Es una recomendación: el
/// coordinador puede modificar el orden libremente mediante las operaciones
/// manuales ya existentes.
/// </summary>
public class PasajeroPropuestoDto
{
    /// <summary>Identificador de la participación del pasajero en el servicio.</summary>
    public int ServicioPasajeroId { get; set; }

    /// <summary>Empleado asociado a esta participación.</summary>
    public int EmpleadoId { get; set; }

    /// <summary>Posición propuesta dentro de la secuencia del servicio (1-based).</summary>
    public int Orden { get; set; }

    /// <summary>
    /// Distancia en línea recta hacia la sede del servicio, en kilómetros.
    /// <c>null</c> si el pasajero o la sede no tienen coordenadas registradas.
    /// </summary>
    public double? DistanciaALaSedeKm { get; set; }
}
