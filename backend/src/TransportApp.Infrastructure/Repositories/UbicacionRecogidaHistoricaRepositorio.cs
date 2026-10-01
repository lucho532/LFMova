using Microsoft.EntityFrameworkCore;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IUbicacionRecogidaHistoricaRepositorio"/> mediante
/// Entity Framework Core. Su responsabilidad es exclusivamente la
/// persistencia; no decide reglas de negocio.
/// </summary>
public class UbicacionRecogidaHistoricaRepositorio : IUbicacionRecogidaHistoricaRepositorio
{
    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public UbicacionRecogidaHistoricaRepositorio(TransportAppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task AgregarAsync(UbicacionRecogidaHistorica ubicacion)
    {
        await _contexto.UbicacionesRecogidaHistorica.AddAsync(ubicacion);
    }

    /// <inheritdoc />
    public async Task<List<UbicacionRecogidaHistorica>> ObtenerPorEmpleadoAsync(int empleadoId)
    {
        return await _contexto.UbicacionesRecogidaHistorica
            .Where(u => u.EmpleadoId == empleadoId)
            .OrderByDescending(u => u.FechaRegistro)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task EliminarPorEmpleadoAsync(int empleadoId)
    {
        var ubicaciones = await _contexto.UbicacionesRecogidaHistorica.Where(u => u.EmpleadoId == empleadoId).ToListAsync();
        _contexto.UbicacionesRecogidaHistorica.RemoveRange(ubicaciones);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
