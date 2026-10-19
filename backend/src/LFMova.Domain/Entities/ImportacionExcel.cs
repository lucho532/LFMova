using LFMova.Domain.Enums;

namespace LFMova.Domain.Entities;

/// <summary>
/// Representa una importación de un archivo Excel realizada por un
/// coordinador para una empresa. Registra el resultado y el estado del
/// procesamiento, pero no contiene la lógica de lectura, validación ni
/// normalización del archivo; eso corresponde a la capa de aplicación.
/// </summary>
public class ImportacionExcel
{
    /// <summary>Identificador único de la importación.</summary>
    public int ImportacionExcelId { get; set; }

    /// <summary>Identificador de la empresa para la que se realizó la importación.</summary>
    public int EmpresaId { get; set; }

    /// <summary>
    /// Identificador del <c>Usuario</c> (con rol COORDINADOR) que realizó la
    /// importación. No existe una entidad independiente <c>Coordinador</c>.
    /// </summary>
    public int? CoordinadorId { get; set; }

    /// <summary>Nombre del archivo importado.</summary>
    public string NombreArchivo { get; set; } = string.Empty;

    /// <summary>Fecha y hora en que se realizó la importación.</summary>
    public DateTime FechaImportacion { get; set; }

    /// <summary>Estado actual de la importación.</summary>
    public EstadoImportacionExcel Estado { get; set; }

    /// <summary>Empresa para la que se realizó la importación.</summary>
    public Empresa? Empresa { get; set; }

    /// <summary>Usuario (coordinador) que realizó la importación.</summary>
    public Usuario? Coordinador { get; set; }
}
