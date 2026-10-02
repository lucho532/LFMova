namespace LFMova.Application.DTOs.Empleados;

/// <summary>
/// Resultado de eliminar a una persona desde la gestión de empleados de una
/// empresa. Indica el alcance real del borrado para que la pantalla lo
/// explique. No contiene datos de la persona eliminada.
/// </summary>
public class ResultadoEliminacionPersonaDto
{
    /// <summary>
    /// <c>true</c> si se borró la cuenta completa (todo rastro de la cédula);
    /// <c>false</c> si la persona también pertenece a otra empresa y solo se
    /// la quitó de esta.
    /// </summary>
    public bool CuentaEliminada { get; set; }
}
