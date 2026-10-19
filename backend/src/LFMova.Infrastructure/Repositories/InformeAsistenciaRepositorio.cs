using Microsoft.EntityFrameworkCore;
using LFMova.Application.DTOs.Soportes;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IInformeAsistenciaRepositorio"/> con una consulta de solo lectura de Entity
/// Framework Core que trae, en una sola ida a la base de datos, cada pasajero con su ruta, conductor y
/// vehículo. No filtra por asistencia ni ordena para el informe: eso lo decide la capa de aplicación.
/// </summary>
public class InformeAsistenciaRepositorio : IInformeAsistenciaRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public InformeAsistenciaRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public Task<List<PasajeroDeRutaDto>> ObtenerPasajerosAsync(int empresaId, DateOnly desde, DateOnly hasta)
    {
        return _contexto.ServiciosPasajero.AsNoTracking()
            .Where(p => p.Servicio!.Jornada!.EmpresaId == empresaId
                && p.Servicio.Estado != EstadoServicio.CANCELADO
                && p.Servicio.Fecha >= desde
                && p.Servicio.Fecha <= hasta)
            .Select(p => new PasajeroDeRutaDto
            {
                Fecha = p.Servicio!.Fecha,
                HoraRuta = p.Servicio.HoraProgramada,
                ServicioId = p.ServicioId,
                JornadaId = p.Servicio.JornadaId,
                Tipo = p.Servicio.Tipo,
                Sede = p.Servicio.Sede!.Nombre,
                Conductor = p.Servicio.UnidadOperativa == null ? null : p.Servicio.UnidadOperativa.Conductor!.Usuario!.NombreCompleto,
                Placa = p.Servicio.UnidadOperativa == null ? null : p.Servicio.UnidadOperativa.Vehiculo!.Placa,
                Cedula = p.Empleado!.Usuario!.Cedula,
                NombreCompleto = p.Empleado.NombreCompleto,
                Telefono = p.Empleado.Telefono,
                // La dirección es la que quedó guardada para esa ruta, no la actual del empleado.
                Direccion = p.DireccionRecogida,
                Barrio = p.Empleado.Barrio,
                Estado = p.Estado,
                Orden = p.Orden,
                HoraLlegadaConductor = p.HoraLlegadaConductor,
                HoraProcesado = p.HoraProcesado
            })
            .ToListAsync();
    }
}
