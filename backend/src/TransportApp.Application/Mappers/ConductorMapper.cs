using TransportApp.Application.DTOs.Conductores;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="Conductor"/>/<see cref="VinculacionConductorEmpresa"/>
/// y sus DTOs. No contiene reglas de negocio ni realiza consultas de
/// persistencia.
/// </summary>
public static class ConductorMapper
{
    /// <summary>
    /// Convierte una entidad <see cref="Conductor"/> en su DTO público.
    /// Requiere que <see cref="Conductor.Usuario"/> y
    /// <see cref="Conductor.VinculacionesConductorEmpresa"/> estén cargados.
    /// </summary>
    public static ConductorDto AConductorDto(Conductor conductor) => new()
    {
        ConductorId = conductor.ConductorId,
        UsuarioId = conductor.UsuarioId,
        Cedula = conductor.Usuario?.Cedula ?? string.Empty,
        NombreCompleto = conductor.NombreCompleto,
        Telefono = conductor.Telefono,
        Activo = conductor.Activo,
        EmpresaIdsVinculadosActivos = conductor.VinculacionesConductorEmpresa
            .Where(v => v.Activa)
            .Select(v => v.EmpresaId)
            .ToList()
    };

    /// <summary>Convierte una entidad <see cref="VinculacionConductorEmpresa"/> en su DTO público.</summary>
    public static VinculacionConductorEmpresaDto AVinculacionDto(VinculacionConductorEmpresa vinculacion) => new()
    {
        VinculacionConductorEmpresaId = vinculacion.VinculacionConductorEmpresaId,
        ConductorId = vinculacion.ConductorId,
        EmpresaId = vinculacion.EmpresaId,
        Activa = vinculacion.Activa
    };
}
