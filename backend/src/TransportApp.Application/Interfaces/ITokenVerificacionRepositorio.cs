using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define las operaciones de persistencia necesarias sobre
/// <see cref="TokenVerificacion"/> para la capa de aplicación. No decide
/// reglas de negocio ni de autorización.
/// </summary>
public interface ITokenVerificacionRepositorio
{
    /// <summary>
    /// Obtiene los tokens no utilizados del tipo indicado para un usuario.
    /// </summary>
    Task<List<TokenVerificacion>> ObtenerNoUtilizadosPorUsuarioAsync(int usuarioId, TipoTokenVerificacion tipo);

    /// <summary>Agrega un nuevo token de verificación.</summary>
    Task AgregarAsync(TokenVerificacion token);

    /// <summary>Persiste los cambios pendientes en el contexto de datos.</summary>
    Task GuardarCambiosAsync();
}
