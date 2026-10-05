using Microsoft.EntityFrameworkCore;
using LFMova.Application.DTOs.Facturacion;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IFacturacionRepositorio"/> con Entity Framework
/// Core. A una misma persona se la identifica por su cédula (no por su
/// identificador de conductor), para contarla una sola vez aunque en el mes
/// se haya eliminado y vuelto a registrar. No decide qué se cobra.
/// </summary>
public class FacturacionRepositorio : IFacturacionRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio con el contexto de datos.</summary>
    public FacturacionRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task AgregarUsoAsync(UsoConductor uso)
    {
        await _contexto.UsosConductor.AddAsync(uso);
    }

    /// <inheritdoc />
    public async Task<List<ResumenFacturacionEmpresaDto>> ObtenerResumenAsync(DateOnly desde, DateOnly hasta)
    {
        var empresas = await _contexto.Empresas.AsNoTracking()
            .OrderBy(e => e.Nombre)
            .Select(e => new ResumenFacturacionEmpresaDto { EmpresaId = e.EmpresaId, NombreEmpresa = e.Nombre })
            .ToListAsync();
        var vinculados = await _contexto.VinculacionesConductorEmpresa.AsNoTracking()
            .Where(v => v.Activa)
            .GroupBy(v => v.EmpresaId)
            .Select(g => new { EmpresaId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.EmpresaId, x => x.Total);
        var usos = await _contexto.UsosConductor.AsNoTracking()
            .Where(u => u.Fecha >= desde && u.Fecha <= hasta)
            .Select(u => new { u.EmpresaId, u.Cedula, u.PasajerosTransportados })
            .ToListAsync();
        var porEmpresa = usos.ToLookup(u => u.EmpresaId);

        foreach (var empresa in empresas)
        {
            var delMes = porEmpresa[empresa.EmpresaId].ToList();
            empresa.ConductoresVinculados = vinculados.GetValueOrDefault(empresa.EmpresaId);
            empresa.ConductoresActivos = delMes.Select(u => u.Cedula).Distinct().Count();
            empresa.RutasFinalizadas = delMes.Count;
            empresa.PasajerosTransportados = delMes.Sum(u => u.PasajerosTransportados);
        }

        return empresas;
    }

    /// <inheritdoc />
    public async Task<List<ConductorFacturableDto>> ObtenerConductoresAsync(int empresaId, DateOnly desde, DateOnly hasta)
    {
        var usos = await _contexto.UsosConductor.AsNoTracking()
            .Where(u => u.EmpresaId == empresaId && u.Fecha >= desde && u.Fecha <= hasta)
            .OrderBy(u => u.Fecha)
            .ToListAsync();
        var cedulas = usos.Select(u => u.Cedula).Distinct().ToList();
        var vigentes = await _contexto.Conductores.AsNoTracking()
            .Where(c => cedulas.Contains(c.Usuario!.Cedula))
            .Select(c => c.Usuario!.Cedula)
            .ToListAsync();

        return usos
            .GroupBy(u => u.Cedula)
            .Select(g => new ConductorFacturableDto
            {
                Cedula = g.Key,
                // El nombre más reciente con el que finalizó una ruta.
                NombreConductor = g.Last().NombreConductor,
                Placas = g.Select(u => u.Placa).Where(p => p.Length > 0).Distinct().ToList(),
                RutasFinalizadas = g.Count(),
                PasajerosTransportados = g.Sum(u => u.PasajerosTransportados),
                PrimeraRuta = g.First().Fecha,
                UltimaRuta = g.Last().Fecha,
                Eliminado = !vigentes.Contains(g.Key)
            })
            .OrderBy(c => c.NombreConductor)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<CierreMensual>> ObtenerCierresAsync(int anio, int mes)
    {
        return await _contexto.CierresMensuales.AsNoTracking().Where(c => c.Anio == anio && c.Mes == mes).ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarCierreAsync(CierreMensual cierre)
    {
        await _contexto.CierresMensuales.AddAsync(cierre);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
