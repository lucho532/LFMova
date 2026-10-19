using LFMova.Application.Utils;

namespace LFMova.Infrastructure.Excel;

/// <summary>
/// Campos que el lector reconoce en una hoja de programación (cédula, nombre, sede, hora de entrada,
/// etc.) y la traducción de cada encabezado escrito por una persona a su campo canónico. No lee celdas
/// ni decide qué formato tiene la hoja.
/// </summary>
internal static class CamposHoja
{
    internal const string Cedula = "CEDULA";
    internal const string Nombre = "NOMBRE";
    internal const string Apellidos = "APELLIDOS";
    internal const string Direccion = "DIRECCION";
    internal const string Barrio = "BARRIO";
    internal const string Celular = "CELULAR";
    internal const string Entrada = "ENTRADA";
    internal const string Salida = "SALIDA";
    internal const string Fecha = "FECHA";
    internal const string Sede = "SEDE";

    /// <summary>Campos sin los que una fila no se considera de encabezados.</summary>
    internal static readonly string[] Obligatorios = { Cedula, Nombre, Direccion, Barrio };

    /// <summary>
    /// Traduce un encabezado a su campo canónico, o <c>null</c> si no se
    /// reconoce. Ignora tildes, mayúsculas, espacios y signos; acepta
    /// sinónimos y tolera un error de tipeo en palabras largas.
    /// </summary>
    internal static string? ReconocerCampo(string encabezado)
    {
        var clave = new string(TextoNormalizador.Normalizar(encabezado).Where(char.IsLetter).ToArray());
        if (clave.Length == 0)
        {
            return null;
        }

        // Una sola columna "Nombre y apellidos" cuenta como nombre completo.
        if (clave.Contains("NOMBRE") && clave.Contains("APELLIDO"))
        {
            return Nombre;
        }

        // El orden importa: "APELLIDO" debe ganar a "NOMBRE" ("nombre y apellido").
        var reglas = new (string Campo, string[] Palabras)[]
        {
            (Apellidos, new[] { "APELLIDO" }),
            (Cedula, new[] { "CEDULA", "DOCUMENTO", "IDENTIFICACION", "CC", "ID" }),
            (Celular, new[] { "CELULAR", "TELEFONO", "MOVIL", "WHATSAPP" }),
            (Direccion, new[] { "DIRECCION", "RECOGIDA", "DOMICILIO" }),
            (Barrio, new[] { "BARRIO", "LOCALIDAD", "ZONA" }),
            (Sede, new[] { "SEDE", "PUNTO", "DESTINO" }),
            (Fecha, new[] { "FECHA", "DIA" }),
            (Entrada, new[] { "ENTRADA", "ENTRA", "INGRESO" }),
            (Salida, new[] { "SALIDA", "SALE", "EGRESO" }),
            (Nombre, new[] { "NOMBRE" }),
        };

        foreach (var (campo, palabras) in reglas)
        {
            foreach (var palabra in palabras)
            {
                if (clave == palabra || (palabra.Length >= 4 && clave.Contains(palabra)) || (palabra.Length >= 6 && Distancia(clave, palabra) <= 1))
                {
                    return campo;
                }
            }
        }

        return null;
    }

    internal static int Distancia(string a, string b)
    {
        if (Math.Abs(a.Length - b.Length) > 1)
        {
            return 2;
        }

        var previa = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var actual = new int[b.Length + 1];
            actual[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                actual[j] = Math.Min(Math.Min(actual[j - 1] + 1, previa[j] + 1), previa[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            }

            previa = actual;
        }

        return previa[b.Length];
    }
}
