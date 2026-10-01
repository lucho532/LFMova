using TransportApp.Application.DTOs.Cuenta;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define los casos de uso sobre la propia cuenta del usuario autenticado:
/// consultar y actualizar sus datos de contacto y cambiar su contraseña. No
/// decide autorización: el usuario se identifica con su token en la capa de Api.
/// </summary>
public interface ICuentaServicio
{
    /// <summary>Obtiene los datos de la cuenta. Falla si el usuario no existe.</summary>
    Task<CuentaDto> ObtenerAsync(int usuarioId);

    /// <summary>Actualiza el nombre y el teléfono de la cuenta.</summary>
    Task<CuentaDto> ActualizarAsync(int usuarioId, ActualizarCuentaDto datos);

    /// <summary>
    /// Cambia la contraseña. Falla si la contraseña actual es incorrecta, si
    /// la nueva no coincide con su confirmación o si es igual a la actual.
    /// </summary>
    Task CambiarContrasenaAsync(int usuarioId, CambiarContrasenaDto datos);
}
