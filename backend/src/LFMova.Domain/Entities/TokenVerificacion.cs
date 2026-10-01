using LFMova.Domain.Enums;

namespace LFMova.Domain.Entities;

/// <summary>
/// Representa un token temporal de un solo uso enviado por correo a un
/// <c>Usuario</c>, utilizado para confirmar su correo o para establecer/
/// restablecer su contraseña (ver <see cref="TipoTokenVerificacion"/>).
/// Reemplaza al antiguo mecanismo de código generado por un conductor: la
/// identidad ahora se confirma por correo electrónico, nunca por un tercero.
/// El token nunca se almacena en texto plano. No decide su propia validez;
/// eso corresponde a la capa de dominio/aplicación.
/// </summary>
public class TokenVerificacion
{
    /// <summary>Identificador único del token.</summary>
    public int TokenVerificacionId { get; set; }

    /// <summary>Usuario al que pertenece este token.</summary>
    public int UsuarioId { get; set; }

    /// <summary>Propósito del token.</summary>
    public TipoTokenVerificacion Tipo { get; set; }

    /// <summary>Hash del token. El token en texto plano nunca se persiste.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Momento en que se generó el token.</summary>
    public DateTime FechaGeneracion { get; set; }

    /// <summary>Momento a partir del cual el token deja de ser válido.</summary>
    public DateTime FechaExpiracion { get; set; }

    /// <summary>Indica si el token ya fue canjeado exitosamente. Un token es de un solo uso.</summary>
    public bool Utilizado { get; set; }

    /// <summary>Usuario al que pertenece este token.</summary>
    public Usuario? Usuario { get; set; }
}
