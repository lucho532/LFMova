using LFMova.Domain.Entities;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="InvitacionEmpresa"/> para la capa de aplicación. No decide
/// reglas de negocio ni de autorización.
/// </summary>
public interface IInvitacionEmpresaRepositorio
{
    /// <summary>Obtiene una invitación por su identificador.</summary>
    Task<InvitacionEmpresa?> ObtenerPorIdAsync(int invitacionEmpresaId);

    /// <summary>Obtiene las invitaciones de una empresa, con el usuario que aceptó cada una (si la aceptó).</summary>
    Task<List<InvitacionEmpresa>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Obtiene las invitaciones de una empresa para una cédula concreta.</summary>
    Task<List<InvitacionEmpresa>> ObtenerPorEmpresaYCedulaAsync(int empresaId, string cedula);

    /// <summary>Agrega una nueva invitación.</summary>
    Task AgregarAsync(InvitacionEmpresa invitacion);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
