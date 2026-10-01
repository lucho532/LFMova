using TransportApp.Domain.Entities;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define la persistencia del registro de importaciones de Excel. No decide
/// reglas de negocio ni de autorización.
/// </summary>
public interface IImportacionExcelRepositorio
{
    /// <summary>Agrega el registro de una importación.</summary>
    Task AgregarAsync(ImportacionExcel importacion);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
