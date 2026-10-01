namespace TransportApp.Application.DTOs.Jornadas;

/// <summary>Representación pública de una jornada.</summary>
public class JornadaDto
{
    /// <summary>Identificador único de la jornada.</summary>
    public int JornadaId { get; set; }

    /// <summary>Empresa a la que pertenece la jornada.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Fecha de referencia del bloque operativo.</summary>
    public DateOnly FechaOperativa { get; set; }
}
