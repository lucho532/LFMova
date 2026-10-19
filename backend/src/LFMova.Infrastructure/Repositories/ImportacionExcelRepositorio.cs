using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IImportacionExcelRepositorio"/> mediante Entity
/// Framework Core. Su responsabilidad es exclusivamente la persistencia.
/// </summary>
public class ImportacionExcelRepositorio : IImportacionExcelRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public ImportacionExcelRepositorio(LFMovaDbContext contexto)
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
