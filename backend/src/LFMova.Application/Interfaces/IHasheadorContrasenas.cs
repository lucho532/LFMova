namespace LFMova.Application.Interfaces;

/// <summary>
/// Define el contrato para generar y verificar hashes seguros de
/// contraseñas. La contraseña nunca se almacena ni se compara en texto
/// plano.
/// </summary>
public interface IHasheadorContrasenas
{
    /// <summary>Genera el hash seguro correspondiente a una contraseña en texto plano.</summary>
    string Hashear(string contrasena);

    /// <summary>Indica si una contraseña en texto plano corresponde al hash almacenado.</summary>
    bool Verificar(string contrasena, string hash);
}
