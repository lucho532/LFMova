using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IImportacionExcelRepositorio"/> mediante Entity
/// Framework Core. Su responsabilidad es exclusivamente la persistencia.
/// </summary>
public class ImportacionExcelRepositorio : IImportacionExcelRepositorio
{
    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public ImportacionExcelRepositorio(TransportAppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task AgregarAsync(ImportacionExcel importacion)
    {
        await _contexto.ImportacionesExcel.AddAsync(importacion);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
