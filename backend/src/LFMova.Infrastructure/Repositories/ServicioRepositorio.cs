using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IServicioRepositorio"/> mediante Entity Framework
/// Core. Su responsabilidad es exclusivamente la persistencia; no decide
/// reglas de negocio.
/// </summary>
public class ServicioRepositorio : IServicioRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public ServicioRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<Servicio?> ObtenerPorIdAsync(int servicioId)
    {
        return await _contexto.Servicios
            .Include(s => s.Jornada)
            .Include(s => s.ServiciosPasajero)
            .Include(s => s.Sede)
            .FirstOrDefaultAsync(s => s.ServicioId == servicioId);
    }

    /// <inheritdoc />
    public async Task<List<Servicio>> ObtenerPorJornadaAsync(int jornadaId)
    {
        return await _contexto.Servicios.Include(s => s.ServiciosPasajero).Include(s => s.Sede).Where(s => s.JornadaId == jornadaId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Servicio>> ObtenerPorUnidadOperativaAsync(int unidadOperativaId)
    {
        return await _contexto.Servicios
            .Include(s => s.Jornada)
            .Include(s => s.ServiciosPasajero)
            .Include(s => s.Sede)
            .Where(s => s.UnidadOperativaId == unidadOperativaId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Servicio>> ObtenerPublicadosPorTipoAsync(TipoServicio tipo)
    {
        return await _contexto.Servicios
            .Include(s => s.Jornada)
            .Include(s => s.ServiciosPasajero)
            .Include(s => s.Sede)
            .Where(s => s.Estado == EstadoServicio.PUBLICADO && s.Tipo == tipo)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Servicio>> ObtenerPendientesDeProgramacionAsync(int empresaId, DateOnly desde)
    {
        return await _contexto.Servicios
            .Include(s => s.ServiciosPasajero)
            .Include(s => s.Sede)
            .Where(s => s.Jornada!.EmpresaId == empresaId
                && s.Estado != EstadoServicio.FINALIZADO && s.Estado != EstadoServicio.CANCELADO
                && (s.Fecha >= desde
                    || s.Estado == EstadoServicio.BORRADOR
                    || s.Estado == EstadoServicio.PENDIENTE_ASIGNACION
                    || s.Estado == EstadoServicio.ASIGNADO))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Servicio>> ObtenerActivosPorFechaYHoraAsync(DateOnly fecha, TimeOnly hora)
    {
        return await _contexto.Servicios
            .Include(s => s.Jornada)
            .Include(s => s.Sede)
            .Where(s => s.Fecha == fecha && s.HoraProgramada == hora
                && (s.Estado == EstadoServicio.PUBLICADO || s.Estado == EstadoServicio.EN_CURSO))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Servicio servicio)
    {
        await _contexto.Servicios.AddAsync(servicio);
    }

    /// <inheritdoc />
    public Task EliminarAsync(Servicio servicio)
    {
        _contexto.Servicios.Remove(servicio);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
