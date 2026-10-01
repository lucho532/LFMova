namespace TransportApp.Application.DTOs.UnidadesOperativas;

/// <summary>Datos necesarios para crear una unidad operativa a partir de un vehículo del conductor.</summary>
public class CrearUnidadOperativaDto
{
    /// <summary>Identificador del vehículo con el que se forma la unidad.</summary>
    public int VehiculoId { get; set; }
}
