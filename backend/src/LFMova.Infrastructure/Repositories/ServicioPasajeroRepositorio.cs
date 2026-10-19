using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IServicioPasajeroRepositorio"/> mediante Entity
/// Framework Core. Su responsabilidad es exclusivamente la persistencia; no
/// decide reglas de negocio.
/// </summary>
public class ServicioPasajeroRepositorio : IServicioPasajeroRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public ServicioPasajeroRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<ServicioPasajero?> ObtenerPorIdAsync(int servicioPasajeroId)
    {
        return await _contexto.ServiciosPasajero.FindAsync(servicioPasajeroId);
    }

    /// <inheritdoc />
    public async Task<ServicioPasajero?> ObtenerPorProgramacionAsync(int programacionTransporteId)
    {
        return await _contexto.ServiciosPasajero
            .FirstOrDefaultAsync(sp => sp.ProgramacionTransporteId == programacionTransporteId);
    }

    /// <inheritdoc />
    public async Task<List<ServicioPasajero>> ObtenerPorServicioAsync(int servicioId)
    {
        return await _contexto.ServiciosPasajero.Where(sp => sp.ServicioId == servicioId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<ServicioPasajero>> ObtenerPorEmpleadoAsync(int empleadoId)
    {
        return await _contexto.ServiciosPasajero
            .Include(sp => sp.Servicio!)
            .ThenInclude(s => s.Jornada)
            .Include(sp => sp.Servicio!)
            .ThenInclude(s => s.UnidadOperativa!)
            .ThenInclude(u => u.Conductor!)
            .ThenInclude(c => c.Usuario)
            .Include(sp => sp.Servicio!)
            .ThenInclude(s => s.UnidadOperativa!)
            .ThenInclude(u => u.Vehiculo)
            .Where(sp => sp.EmpleadoId == empleadoId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(ServicioPasajero servicioPasajero)
    {
        await _contexto.ServiciosPasajero.AddAsync(servicioPasajero);
    }

    /// <inheritdoc />
    public Task EliminarAsync(ServicioPasajero servicioPasajero)
    {
        _contexto.ServiciosPasajero.Remove(servicioPasajero);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Descartar(ServicioPasajero servicioPasajero)
    {
        _contexto.Entry(servicioPasajero).State = EntityState.Detached;
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
