using Microsoft.EntityFrameworkCore;
using TransportApp.Application.DTOs.Estadisticas;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Infrastructure.Data;

namespace TransportApp.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IEstadisticasRepositorio"/> con consultas de solo
/// lectura de Entity Framework Core. Un pasajero cuenta como transportado
/// cuando su estado es <see cref="EstadoServicioPasajero.RECOGIDO"/> (el conductor solo registra llegada y resultado).
/// </summary>
public class EstadisticasRepositorio : IEstadisticasRepositorio
{
    private const int DiasEnGrafica = 14;

    private readonly TransportAppDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public EstadisticasRepositorio(TransportAppDbContext contexto)
    {
        _contexto = contexto;
    }

    private IQueryable<Servicio> ServiciosDeEmpresa(int empresaId)
        => _contexto.Servicios.AsNoTracking().Where(s => s.Jornada!.EmpresaId == empresaId);

    /// <inheritdoc />
    public async Task<ResumenEmpresaDto> ObtenerResumenAsync(int empresaId)
    {
        var servicios = await ServiciosDeEmpresa(empresaId)
            .Select(s => new
            {
                s.Fecha,
                s.Estado,
                Sede = s.Sede!.Nombre,
                Pasajeros = s.ServiciosPasajero.Count,
                Transportados = s.ServiciosPasajero.Count(p => p.Estado == EstadoServicioPasajero.RECOGIDO)
            })
            .ToListAsync();

        var vigentes = servicios.Where(s => s.Estado != EstadoServicio.CANCELADO).ToList();
        // "Programada" solo cuenta desde que el coordinador la publica: antes de eso es trabajo interno de armado de rutas, no una ruta real todavía.
        var programadas = vigentes.Where(s => s.Estado is EstadoServicio.PUBLICADO or EstadoServicio.EN_CURSO).ToList();

        return new ResumenEmpresaDto
        {
            RutasProgramadas = programadas.Count,
            RutasRealizadas = vigentes.Count(s => s.Estado == EstadoServicio.FINALIZADO),
            RutasCanceladas = servicios.Count - vigentes.Count,
            RutasPorRealizar = vigentes.Count(s => s.Estado != EstadoServicio.FINALIZADO),
            PasajerosProgramados = programadas.Sum(s => s.Pasajeros),
            PasajerosTransportados = vigentes.Sum(s => s.Transportados),
            Conductores = await _contexto.VinculacionesConductorEmpresa.CountAsync(v => v.EmpresaId == empresaId && v.Activa),
            Empleados = await _contexto.Empleados.CountAsync(e => e.EmpresaId == empresaId && e.Activo),
            Sedes = await _contexto.Sedes.CountAsync(s => s.EmpresaId == empresaId && s.Activa),
            UltimosDias = programadas
                .GroupBy(s => s.Fecha)
                .OrderByDescending(g => g.Key)
                .Take(DiasEnGrafica)
                .OrderBy(g => g.Key)
                .Select(g => new RutasDiaDto { Fecha = g.Key, Rutas = g.Count(), PasajerosTransportados = g.Sum(s => s.Transportados) })
                .ToList(),
            PorSede = programadas
                .GroupBy(s => s.Sede)
                .OrderByDescending(g => g.Count())
                .Select(g => new RutasSedeDto { Sede = g.Key, Rutas = g.Count(), PasajerosTransportados = g.Sum(s => s.Transportados) })
                .ToList()
        };
    }

    /// <inheritdoc />
    public async Task<List<EstadisticaConductorDto>> ObtenerPorConductorAsync(int empresaId, DateOnly? desde, DateOnly? hasta)
    {
        var conductores = await _contexto.Conductores.AsNoTracking()
            .Where(c => c.VinculacionesConductorEmpresa.Any(v => v.EmpresaId == empresaId))
            .Select(c => new
            {
                c.ConductorId,
                c.Usuario!.Cedula,
                c.Usuario.NombreCompleto,
                c.Usuario.Telefono,
                Vehiculos = c.Vehiculos.Where(v => v.Activo).Select(v => new VehiculoResumenDto
                {
                    Placa = v.Placa,
                    Marca = v.Marca,
                    Modelo = v.Modelo,
                    Capacidad = v.Capacidad,
                    VigenciaSoat = v.VigenciaSoat,
                    VigenciaTecnomecanica = v.VigenciaTecnomecanica
                }).ToList()
            })
            .ToListAsync();

        var servicios = await FiltrarPorFecha(ServiciosDeEmpresa(empresaId), desde, hasta)
            .Where(s => s.UnidadOperativaId != null && s.Estado != EstadoServicio.CANCELADO)
            .Select(s => new
            {
                s.UnidadOperativa!.ConductorId,
                s.Estado,
                Transportados = s.ServiciosPasajero.Count(p => p.Estado == EstadoServicioPasajero.RECOGIDO)
            })
            .ToListAsync();

        return conductores
            .Select(c =>
            {
                var suyos = servicios.Where(s => s.ConductorId == c.ConductorId).ToList();
                return new EstadisticaConductorDto
                {
                    ConductorId = c.ConductorId,
                    Cedula = c.Cedula,
                    NombreCompleto = c.NombreCompleto,
                    Telefono = c.Telefono ?? string.Empty,
                    Vehiculos = c.Vehiculos,
                    RutasProgramadas = suyos.Count,
                    RutasRealizadas = suyos.Count(s => s.Estado == EstadoServicio.FINALIZADO),
                    PasajerosTransportados = suyos.Sum(s => s.Transportados)
                };
            })
            .OrderBy(c => c.NombreCompleto)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<RutaConductorDto>> ObtenerRutasDeConductorAsync(int empresaId, int conductorId, DateOnly? desde, DateOnly? hasta)
    {
        var rutas = await FiltrarPorFecha(ServiciosDeEmpresa(empresaId), desde, hasta)
            .Where(s => s.UnidadOperativa!.ConductorId == conductorId && s.Estado != EstadoServicio.CANCELADO)
            .OrderByDescending(s => s.Fecha).ThenByDescending(s => s.HoraProgramada)
            .Select(s => new RutaConductorDto
            {
                ServicioId = s.ServicioId,
                JornadaId = s.JornadaId,
                Fecha = s.Fecha,
                Hora = s.HoraProgramada,
                Tipo = (int)s.Tipo,
                Estado = (int)s.Estado,
                Sede = s.Sede!.Nombre,
                Pasajeros = s.ServiciosPasajero.Count,
                PasajerosTransportados = s.ServiciosPasajero.Count(p => p.Estado == EstadoServicioPasajero.RECOGIDO)
            })
            .ToListAsync();

        return rutas;
    }

    /// <inheritdoc />
    public async Task<List<RutaEmpresaDto>> ObtenerRutasAsync(int empresaId, string? filtro, DateOnly? desde, DateOnly? hasta)
    {
        var consulta = FiltrarPorFecha(ServiciosDeEmpresa(empresaId), desde, hasta);
        consulta = filtro switch
        {
            "programadas" => consulta.Where(s => s.Estado == EstadoServicio.PUBLICADO || s.Estado == EstadoServicio.EN_CURSO),
            "realizadas" => consulta.Where(s => s.Estado == EstadoServicio.FINALIZADO),
            "por-realizar" => consulta.Where(s => s.Estado != EstadoServicio.CANCELADO && s.Estado != EstadoServicio.FINALIZADO),
            "canceladas" => consulta.Where(s => s.Estado == EstadoServicio.CANCELADO),
            _ => consulta
        };

        return await consulta
            .OrderByDescending(s => s.Fecha).ThenBy(s => s.HoraProgramada)
            .Select(s => new RutaEmpresaDto
            {
                ServicioId = s.ServicioId,
                JornadaId = s.JornadaId,
                Fecha = s.Fecha,
                Hora = s.HoraProgramada,
                Tipo = (int)s.Tipo,
                Estado = (int)s.Estado,
                Sede = s.Sede!.Nombre,
                UnidadOperativaId = s.UnidadOperativaId,
                Conductor = s.UnidadOperativa == null ? null : s.UnidadOperativa.Conductor!.Usuario!.NombreCompleto,
                Placa = s.UnidadOperativa == null ? null : s.UnidadOperativa.Vehiculo!.Placa,
                Pasajeros = s.ServiciosPasajero.Count,
                PasajerosTransportados = s.ServiciosPasajero.Count(p => p.Estado == EstadoServicioPasajero.RECOGIDO)
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<PasajeroEmpresaDto>> ObtenerPasajerosAsync(int empresaId, bool soloTransportados, DateOnly? desde, DateOnly? hasta)
    {
        var consulta = _contexto.ServiciosPasajero.AsNoTracking()
            .Where(p => p.Servicio!.Jornada!.EmpresaId == empresaId && p.Servicio.Estado != EstadoServicio.CANCELADO);
        if (soloTransportados)
        {
            consulta = consulta.Where(p => p.Estado == EstadoServicioPasajero.RECOGIDO);
        }

        if (desde is not null)
        {
            consulta = consulta.Where(p => p.Servicio!.Fecha >= desde);
        }

        if (hasta is not null)
        {
            consulta = consulta.Where(p => p.Servicio!.Fecha <= hasta);
        }

        return await consulta
            .OrderByDescending(p => p.Servicio!.Fecha).ThenBy(p => p.Servicio!.HoraProgramada).ThenBy(p => p.Orden)
            .Take(2000)
            .Select(p => new PasajeroEmpresaDto
            {
                Cedula = p.Empleado!.Usuario!.Cedula,
                NombreCompleto = p.Empleado.NombreCompleto,
                Telefono = p.Empleado.Telefono,
                ServicioId = p.ServicioId,
                JornadaId = p.Servicio!.JornadaId,
                Fecha = p.Servicio.Fecha,
                Hora = p.Servicio.HoraProgramada,
                Tipo = (int)p.Servicio.Tipo,
                Sede = p.Servicio.Sede!.Nombre,
                Estado = (int)p.Estado,
                Conductor = p.Servicio.UnidadOperativa == null ? null : p.Servicio.UnidadOperativa.Conductor!.Usuario!.NombreCompleto
            })
            .ToListAsync();
    }

    private static IQueryable<Servicio> FiltrarPorFecha(IQueryable<Servicio> consulta, DateOnly? desde, DateOnly? hasta)
    {
        if (desde is not null)
        {
            consulta = consulta.Where(s => s.Fecha >= desde);
        }

        if (hasta is not null)
        {
            consulta = consulta.Where(s => s.Fecha <= hasta);
        }

        return consulta;
    }
}
