namespace LFMova.Domain.Entities;

/// <summary>
/// Número con el que una empresa identifica internamente a una unidad operativa (su vehículo), por
/// ejemplo "103". Es propio de cada empresa: un mismo vehículo puede trabajar para varias empresas y
/// cada una lo numera a su manera, por eso no vive en <c>Vehiculo</c> ni en <c>UnidadOperativa</c>.
/// Una unidad tiene como máximo un número por empresa y dos unidades de la misma empresa no pueden
/// compartirlo. No tiene reglas de negocio propias.
/// </summary>
public class NumeroInternoUnidad
{
    /// <summary>Identificador único del registro.</summary>
    public int NumeroInternoUnidadId { get; set; }

    /// <summary>Identificador de la empresa que usa el número.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Identificador de la unidad operativa a la que se le asigna.</summary>
    public int UnidadOperativaId { get; set; }

    /// <summary>Número interno tal como lo escribe la empresa (sin espacios a los lados).</summary>
    public string Numero { get; set; } = string.Empty;

    /// <summary>Empresa que usa el número.</summary>
    public Empresa? Empresa { get; set; }

    /// <summary>Unidad operativa numerada.</summary>
    public UnidadOperativa? UnidadOperativa { get; set; }
}
