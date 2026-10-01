using TransportApp.Application.DTOs.Sedes;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="Sede"/> y sus DTOs. No contiene reglas de
/// negocio ni realiza consultas de persistencia.
/// </summary>
public static class SedeMapper
{
    /// <summary>Convierte una entidad <see cref="Sede"/> en su DTO público.</summary>
    public static SedeDto ASedeDto(Sede sede) => new()
    {
        SedeId = sede.SedeId,
        EmpresaId = sede.EmpresaId,
        Nombre = sede.Nombre,
        Direccion = sede.Direccion,
        Ciudad = sede.Ciudad,
        Barrio = sede.Barrio,
        Latitud = sede.Latitud,
        Longitud = sede.Longitud,
        Activa = sede.Activa
    };
}
