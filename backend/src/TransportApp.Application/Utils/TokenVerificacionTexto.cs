using System.Security.Cryptography;

namespace TransportApp.Application.Utils;

/// <summary>
/// Construye y desarma el texto plano de un token de verificación enviado
/// por correo. El token codifica el <c>UsuarioId</c> en claro (no es
/// secreto: solo identifica a quién pertenece) seguido de un componente
/// aleatorio que sí es secreto y que se almacena únicamente como hash (ver
/// <see cref="TransportApp.Domain.Entities.TokenVerificacion.TokenHash"/>).
/// Esto permite validar el token sin tener que recorrer todos los tokens
/// pendientes de la plataforma.
/// </summary>
public static class TokenVerificacionTexto
{
    /// <summary>Genera el texto plano de un nuevo token para el usuario indicado.</summary>
    public static string Generar(int usuarioId)
        => $"{usuarioId}.{Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))}";

    /// <summary>
    /// Extrae el <c>UsuarioId</c> codificado en el token, o <c>null</c> si el
    /// texto no tiene el formato esperado.
    /// </summary>
    public static int? ExtraerUsuarioId(string tokenTextoPlano)
    {
        var separador = tokenTextoPlano.IndexOf('.');
        if (separador <= 0)
        {
            return null;
        }

        return int.TryParse(tokenTextoPlano[..separador], out var usuarioId) ? usuarioId : null;
    }
}
