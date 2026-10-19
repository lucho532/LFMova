namespace LFMova.Application.DTOs.UnidadesOperativas;

/// <summary>Número interno que una empresa le asignó a una de sus unidades operativas.</summary>
public class NumeroInternoUnidadDto
{
    /// <summary>Identificador de la unidad operativa.</summary>
    public int UnidadOperativaId { get; set; }

    /// <summary>Número interno de la empresa, por ejemplo "103".</summary>
    public string Numero { get; set; } = string.Empty;
}

/// <summary>Datos para asignar o quitar el número interno de una unidad.</summary>
public class AsignarNumeroInternoDto
{
    /// <summary>Número interno nuevo; vacío o <c>null</c> para quitarlo.</summary>
    public string? Numero { get; set; }
}
