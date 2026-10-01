namespace TransportApp.Application.DTOs.Jornadas;

/// <summary>Datos necesarios para crear una jornada.</summary>
public class CrearJornadaDto
{
    /// <summary>Fecha de referencia del bloque operativo.</summary>
    public DateOnly FechaOperativa { get; set; }
}
