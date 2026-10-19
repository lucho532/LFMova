namespace LFMova.Domain.Exceptions;

/// <summary>
/// Se lanza cuando se intenta guardar una zona que combina dos barrios
/// entre los que existe una <see cref="Entities.BarreraGeografica"/>
/// declarada. Hereda de <see cref="InvalidOperationException"/> para que el
/// código existente que captura esa excepción siga funcionando, pero permite
/// a la capa de Api distinguir este caso (HTTP 409) de un simple "no existe"
/// (HTTP 404).
/// </summary>
public class BarreraGeograficaConflictoException : InvalidOperationException
{
    /// <summary>Crea la excepción con el mensaje que se mostrará al coordinador.</summary>
    public BarreraGeograficaConflictoException(string mensaje) : base(mensaje)
    {
    }
}
