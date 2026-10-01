using TransportApp.Application.DTOs.Empresas;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="Empresa"/> y sus DTOs. No contiene reglas de
/// negocio ni realiza consultas de persistencia.
/// </summary>
public static class EmpresaMapper
{
    /// <summary>
    /// Convierte una entidad <see cref="Empresa"/> en su DTO público.
    /// <paramref name="coordinadorPrincipal"/> es la cédula del primer
    /// coordinador activo, cuando se dispone de ella (listado general).
    /// </summary>
    public static EmpresaDto AEmpresaDto(Empresa empresa, string? coordinadorPrincipal = null, string? coordinadorPrincipalNombre = null) => new()
    {
        EmpresaId = empresa.EmpresaId,
        Nombre = empresa.Nombre,
        Cif = empresa.Cif,
        Direccion = empresa.Direccion,
        CoordinadorPrincipal = coordinadorPrincipal,
        CoordinadorPrincipalNombre = coordinadorPrincipalNombre,
        Activa = empresa.Activa
    };
}
