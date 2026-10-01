namespace TransportApp.Application.Utils;

/// <summary>
/// Forma única en que se guarda y se compara un correo electrónico: sin
/// espacios alrededor y en minúsculas. Así "Ana@Correo.com" y
/// "ana@correo.com" son la misma cuenta al registrarse, al iniciar sesión, al
/// recuperar la contraseña o al invitar. No valida el formato: eso lo hace
/// <c>EmailValidador</c>.
/// </summary>
public static class CorreoNormalizador
{
    /// <summary>Devuelve el correo sin espacios alrededor y en minúsculas.</summary>
    public static string Normalizar(string correo) => correo.Trim().ToLowerInvariant();
}
