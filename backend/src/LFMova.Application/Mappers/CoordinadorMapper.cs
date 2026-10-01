using LFMova.Application.DTOs.Empresas;
using LFMova.Domain.Entities;

namespace LFMova.Application.Mappers;

/// <summary>
/// Convierte un <see cref="UsuarioRol"/> de rol COORDINADOR en su DTO
/// público. No contiene reglas de negocio ni realiza consultas de
/// persistencia.
/// </summary>
public static class CoordinadorMapper
{
    /// <summary>
    /// Convierte un <see cref="UsuarioRol"/> COORDINADOR (con su
    /// <see cref="Usuario"/> cargado) en <see cref="CoordinadorDto"/>.
    /// </summary>
    public static CoordinadorDto ACoordinadorDto(UsuarioRol usuarioRol) => new()
    {
        UsuarioRolId = usuarioRol.UsuarioRolId,
        Cedula = usuarioRol.Usuario?.Cedula ?? string.Empty,
        NombreCompleto = usuarioRol.Usuario?.NombreCompleto ?? string.Empty,
        Email = usuarioRol.Usuario?.Email,
        Telefono = usuarioRol.Usuario?.Telefono ?? string.Empty,
        Activo = usuarioRol.Activo
    };
}
