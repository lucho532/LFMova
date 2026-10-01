using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IProgramacionTransporteRepositorio"/> mediante
/// Entity Framework Core. Su responsabilidad es exclusivamente la
/// persistencia; no decide reglas de negocio.
/// </summary>
public class ProgramacionTransporteRepositorio : IProgramacionTransporteRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public ProgramacionTransporteRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<ProgramacionTransporte?> ObtenerPorIdAsync(int programacionTransporteId)
    {
        return await _contexto.ProgramacionesTransporte.FindAsync(programacionTransporteId);
    }

    /// <inheritdoc />
    public async Task<List<ProgramacionTransporte>> ObtenerPorEmpresaAsync(int empresaId)
    {
        return await _contexto.ProgramacionesTransporte.Where(p => p.EmpresaId == empresaId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ProgramacionTransporte?> ObtenerPorClaveAsync(
        int empleadoId, int sedeId, DateOnly fecha, TimeOnly hora, TipoServicio tipo)
    {
        return await _contexto.ProgramacionesTransporte.FirstOrDefaultAsync(p =>
            p.EmpleadoId == empleadoId && p.SedeId == sedeId && p.Fecha == fecha && p.Hora == hora && p.Tipo == tipo);
    }

    /// <inheritdoc />
    public async Task AgregarAsync(ProgramacionTransporte programacion)
    {
        await _contexto.ProgramacionesTransporte.AddAsync(programacion);
    }

    /// <inheritdoc />
    public void Descartar(ProgramacionTransporte programacion)
    {
        _contexto.Entry(programacion).State = EntityState.Detached;
    }

    /// <inheritdoc />
    public Task EliminarAsync(ProgramacionTransporte programacion)
    {
        _contexto.ProgramacionesTransporte.Remove(programacion);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
