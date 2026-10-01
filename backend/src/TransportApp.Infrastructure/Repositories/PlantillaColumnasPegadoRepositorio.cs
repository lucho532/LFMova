using Microsoft.EntityFrameworkCore;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IPlantillaColumnasPegadoRepositorio"/> mediante
/// Entity Framework Core. Su responsabilidad es exclusivamente la
/// persistencia; no decide reglas de negocio.
/// </summary>
public class PlantillaColumnasPegadoRepositorio : IPlantillaColumnasPegadoRepositorio
{
    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public PlantillaColumnasPegadoRepositorio(TransportAppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<PlantillaColumnasPegado?> ObtenerPorEmpresaAsync(int empresaId)
    {
        return await _contexto.PlantillasColumnasPegado.FirstOrDefaultAsync(p => p.EmpresaId == empresaId);
    }

    /// <inheritdoc />
    public async Task AgregarAsync(PlantillaColumnasPegado plantilla)
    {
        await _contexto.PlantillasColumnasPegado.AddAsync(plantilla);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
