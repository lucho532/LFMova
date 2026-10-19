namespace LFMova.Application.DTOs.Importaciones;

/// <summary>Refleja la plantilla de orden de columnas de una empresa para crear rutas pegando filas de Excel.</summary>
public class PlantillaColumnasPegadoDto
{
    /// <summary>Identificador de la plantilla.</summary>
    public int PlantillaColumnasPegadoId { get; set; }

    /// <summary>Campos, en el orden en que vienen en el Excel externo del coordinador.</summary>
    public List<string> ColumnasEnOrden { get; set; } = new();
}
