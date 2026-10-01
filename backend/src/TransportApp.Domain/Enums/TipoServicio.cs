namespace TransportApp.Domain.Enums;

/// <summary>
/// Representa el sentido del transporte de una <c>ProgramacionTransporte</c> o
/// de un <c>Servicio</c>: si el empleado se dirige hacia la sede o la sede se
/// dirige hacia el empleado.
/// No debe utilizarse para representar estados operativos.
/// </summary>
public enum TipoServicio
{
    /// <summary>El empleado se transporta desde su ubicación de recogida hacia la sede.</summary>
    ENTRADA,

    /// <summary>El empleado se transporta desde la sede hacia su dirección.</summary>
    SALIDA
}
