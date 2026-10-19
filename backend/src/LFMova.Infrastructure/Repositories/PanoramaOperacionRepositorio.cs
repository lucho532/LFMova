using Microsoft.EntityFrameworkCore;
using LFMova.Application.DTOs.Estadisticas;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IPanoramaOperacionRepositorio"/> con una única consulta de solo lectura que cubre el
/// periodo pedido y el periodo anterior de igual duración. Un pasajero cuenta como transportado cuando su
/// estado es <see cref="EstadoServicioPasajero.RECOGIDO"/>. No valida el rango ni la autorización.
/// </summary>
public class PanoramaOperacionRepositorio : IPanoramaOperacionRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public PanoramaOperacionRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<PanoramaOperacionDto> ObtenerAsync(int empresaId, DateOnly desde, DateOnly hasta)
    {
        var dias = hasta.DayNumber - desde.DayNumber + 1;
        var desdeAnterior = desde.AddDays(-dias);

        var servicios = await _contexto.Servicios.AsNoTracking()
            .Where(s => s.Jornada!.EmpresaId == empresaId && s.Estado != EstadoServicio.CANCELADO
                        && s.Fecha >= desdeAnterior && s.Fecha <= hasta)
            .Select(s => new
            {
                s.Fecha,
                s.Tipo,
                s.Estado,
                ConductorId = s.UnidadOperativa == null ? (int?)null : s.UnidadOperativa.ConductorId,
                Recogidos = s.ServiciosPasajero.Count(p => p.Estado == EstadoServicioPasajero.RECOGIDO),
                NoAsistieron = s.ServiciosPasajero.Count(p => p.Estado == EstadoServicioPasajero.NO_RECOGIDO || p.Estado == EstadoServicioPasajero.NO_ASISTIRA)
            })
            .ToListAsync();

        var delPeriodo = servicios.Where(s => s.Fecha >= desde).ToList();
        var finalizados = delPeriodo.Where(s => s.Estado == EstadoServicio.FINALIZADO).ToList();
        var transportados = delPeriodo.Sum(s => s.Recogidos);
        var transportadosAnterior = servicios.Where(s => s.Fecha < desde).Sum(s => s.Recogidos);

        return new PanoramaOperacionDto
        {
            Desde = desde,
            Hasta = hasta,
            Serie = Enumerable.Range(0, dias)
                .Select(i => desde.AddDays(i))
                .Select(dia => new PasajerosDiaDto
                {
                    Fecha = dia,
                    Entradas = delPeriodo.Where(s => s.Fecha == dia && s.Tipo == TipoServicio.ENTRADA).Sum(s => s.Recogidos),
                    Salidas = delPeriodo.Where(s => s.Fecha == dia && s.Tipo == TipoServicio.SALIDA).Sum(s => s.Recogidos)
                })
                .ToList(),
            EmpleadosTransportados = transportados,
            VariacionEmpleadosTransportados = ReglasPanoramaOperacion.VariacionPorcentual(transportados, transportadosAnterior),
            ServiciosRealizados = finalizados.Count,
            ServiciosEntrada = finalizados.Count(s => s.Tipo == TipoServicio.ENTRADA),
            ServiciosSalida = finalizados.Count(s => s.Tipo == TipoServicio.SALIDA),
            ConductoresEnOperacion = delPeriodo
                .Where(s => s.ConductorId != null && (s.Estado is EstadoServicio.PUBLICADO or EstadoServicio.EN_CURSO or EstadoServicio.FINALIZADO))
                .Select(s => s.ConductorId).Distinct().Count(),
            PromedioAsistencia = ReglasPanoramaOperacion.PromedioAsistencia(finalizados.Sum(s => s.Recogidos), finalizados.Sum(s => s.NoAsistieron))
        };
    }
}
