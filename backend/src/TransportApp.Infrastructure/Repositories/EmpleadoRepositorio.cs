using Microsoft.EntityFrameworkCore;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IEmpleadoRepositorio"/> mediante Entity Framework
/// Core. Su responsabilidad es exclusivamente la persistencia; no decide
/// reglas de negocio.
/// </summary>
public class EmpleadoRepositorio : IEmpleadoRepositorio
{
    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public EmpleadoRepositorio(TransportAppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<Empleado?> ObtenerPorIdAsync(int empleadoId)
    {
        return await _contexto.Empleados
            .Include(e => e.Usuario)
            .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId);
    }

    /// <inheritdoc />
    public async Task<Empleado?> ObtenerPorUsuarioIdAsync(int usuarioId)
    {
        return await _contexto.Empleados
            .Include(e => e.Usuario)
            .FirstOrDefaultAsync(e => e.UsuarioId == usuarioId);
    }

    /// <inheritdoc />
    public async Task<List<Empleado>> ObtenerPorEmpresaAsync(int empresaId)
    {
        return await _contexto.Empleados
            .Include(e => e.Usuario)
            .Where(e => e.EmpresaId == empresaId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Empleado empleado)
    {
        await _contexto.Empleados.AddAsync(empleado);
    }

    /// <inheritdoc />
    public void Descartar(Empleado empleado)
    {
        _contexto.Entry(empleado).State = EntityState.Detached;
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
