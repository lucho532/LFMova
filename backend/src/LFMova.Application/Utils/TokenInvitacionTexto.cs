using System.Security.Cryptography;

namespace LFMova.Application.Utils;

/// <summary>
/// Construye y desarma el texto plano del token de una invitación a una
/// empresa. Igual que <see cref="TokenVerificacionTexto"/>, codifica el
/// identificador de la invitación en claro (solo identifica cuál es) seguido
/// de un componente aleatorio secreto que se almacena únicamente como hash.
/// </summary>
public static class TokenInvitacionTexto
{
    /// <summary>Genera el texto plano de un nuevo token para la invitación indicada.</summary>
    public static string Generar(int invitacionEmpresaId)
        => $"{invitacionEmpresaId}.{Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))}";

    /// <summary>Extrae el identificador de la invitación codificado en el token, o <c>null</c> si el formato no es válido.</summary>
    public static int? ExtraerInvitacionId(string tokenTextoPlano)
    {
        var separador = tokenTextoPlano.IndexOf('.');
        if (separador <= 0)
        {
            return null;
        }

        return int.TryParse(tokenTextoPlano[..separador], out var id) ? id : null;
    }
}
