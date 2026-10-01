using System.Security.Cryptography;
using LFMova.Application.Interfaces;

namespace LFMova.Application.Utils;

/// <summary>
/// Implementa <see cref="IHasheadorContrasenas"/> mediante PBKDF2
/// (<see cref="Rfc2898DeriveBytes"/>) mediante la API estándar de
/// <c>System.Security.Cryptography</c>. No introduce dependencias externas
/// adicionales ni un sistema completo de identidad.
/// </summary>
public class HasheadorContrasenas : IHasheadorContrasenas
{
    private const int TamanoSal = 16;
    private const int TamanoHash = 32;
    private const int Iteraciones = 100_000;
    private static readonly HashAlgorithmName Algoritmo = HashAlgorithmName.SHA256;

    /// <inheritdoc />
    public string Hashear(string contrasena)
    {
        var sal = RandomNumberGenerator.GetBytes(TamanoSal);
        var hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, Algoritmo, TamanoHash);

        return $"{Iteraciones}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    /// <inheritdoc />
    public bool Verificar(string contrasena, string hash)
    {
        var partes = hash.Split('.', 3);
        if (partes.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(partes[0], out var iteraciones))
        {
            return false;
        }

        var sal = Convert.FromBase64String(partes[1]);
        var hashEsperado = Convert.FromBase64String(partes[2]);

        var hashCalculado = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iteraciones, Algoritmo, hashEsperado.Length);

        return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
    }
}
