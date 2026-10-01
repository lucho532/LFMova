using TransportApp.Application.DTOs.Vehiculos;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="Vehiculo"/> y su DTO. No contiene reglas de
/// negocio ni realiza consultas de persistencia.
/// </summary>
public static class VehiculoMapper
{
    /// <summary>Convierte una entidad <see cref="Vehiculo"/> en su DTO público.</summary>
    public static VehiculoDto AVehiculoDto(Vehiculo vehiculo) => new()
    {
        VehiculoId = vehiculo.VehiculoId,
        ConductorId = vehiculo.ConductorId,
        Placa = vehiculo.Placa,
        Marca = vehiculo.Marca,
        Modelo = vehiculo.Modelo,
        Capacidad = vehiculo.Capacidad,
        VigenciaSoat = vehiculo.VigenciaSoat,
        VigenciaTecnomecanica = vehiculo.VigenciaTecnomecanica,
        Activo = vehiculo.Activo
    };
}
