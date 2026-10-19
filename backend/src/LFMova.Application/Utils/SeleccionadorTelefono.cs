namespace LFMova.Application.Utils;

/// <summary>
/// Decide qué teléfono usar cuando un archivo trae varias columnas de contacto (por ejemplo «TELEFONO» casi
/// siempre en 0 y «CELULAR» con el número real). Prefiere un celular colombiano (10 dígitos que empiezan por 3)
/// y descarta los vacíos y los de solo ceros. No valida nada más ni consulta datos.
/// </summary>
public static class SeleccionadorTelefono
{
    private const int DigitosMinimos = 7;
    private const int DigitosCelular = 10;

    /// <summary>Indica si el valor es un teléfono utilizable: al menos 7 dígitos y no solo ceros.</summary>
    public static bool EsUtil(string? telefono)
    {
        var digitos = SoloDigitos(telefono);
        return digitos.Length >= DigitosMinimos && digitos.Any(d => d != '0');
    }

    /// <summary>Indica si el valor es un celular colombiano: 10 dígitos que empiezan por 3.</summary>
    public static bool EsCelular(string? telefono)
    {
        var digitos = SoloDigitos(telefono);
        return digitos.Length == DigitosCelular && digitos[0] == '3';
    }

    /// <summary>
    /// Indica si el teléfono <paramref name="nuevo"/> que trae un archivo debe reemplazar al <paramref name="actual"/>:
    /// el nuevo sirve y es distinto, salvo que cambiaría un celular por un fijo (un archivo que solo trae el fijo
    /// no debe quitar el celular). Así un número que cambió se actualiza y los vacíos o en cero se llenan.
    /// </summary>
    public static bool DebeReemplazar(string? actual, string? nuevo)
    {
        if (!EsUtil(nuevo))
        {
            return false;
        }

        if (!EsUtil(actual))
        {
            return true;
        }

        return SoloDigitos(actual) != SoloDigitos(nuevo) && !(EsCelular(actual) && !EsCelular(nuevo));
    }

    /// <summary>
    /// Elige el mejor teléfono de los candidatos: primero un celular (10 dígitos, empieza por 3), luego
    /// cualquier número utilizable. Devuelve vacío si ninguno sirve.
    /// </summary>
    public static string ElegirMejor(IEnumerable<string> candidatos)
    {
        var utiles = candidatos.Select(SoloDigitos).Where(c => EsUtil(c)).ToList();
        return utiles.FirstOrDefault(c => EsCelular(c)) ?? utiles.FirstOrDefault() ?? string.Empty;
    }

    private static string SoloDigitos(string? texto) => new((texto ?? string.Empty).Where(char.IsDigit).ToArray());
}
