using System.Globalization;
using System.Text;
using LFMova.Domain.Entities;

namespace LFMova.Domain.Rules;

/// <summary>
/// Regla de dominio que decide si dos barrios tienen una barrera geográfica
/// declarada entre sí (relación simétrica, sin importar el orden), a partir
/// de la lista de <see cref="BarreraGeografica"/> de la empresa. Su único uso
/// es impedir que dos barrios con barrera queden dentro de la misma zona. No
/// accede a persistencia ni calcula distancias ni rutas.
/// </summary>
public static class ReglasBarrerasGeograficas
{
    /// <summary>Indica si existe una barrera declarada entre <paramref name="barrioA"/> y <paramref name="barrioB"/>.</summary>
    public static bool HayBarreraEntre(IEnumerable<BarreraGeografica> barreras, string barrioA, string barrioB)
    {
        var a = Normalizar(barrioA);
        var b = Normalizar(barrioB);
        return barreras.Any(barrera =>
        {
            var ba = Normalizar(barrera.BarrioA);
            var bb = Normalizar(barrera.BarrioB);
            return (ba == a && bb == b) || (ba == b && bb == a);
        });
    }

    /// <summary>
    /// Recorre todas las combinaciones de <paramref name="barrios"/> y
    /// devuelve el primer par que tiene una barrera declarada entre sí, o
    /// <c>null</c> si ninguno la tiene.
    /// </summary>
    public static (string BarrioA, string BarrioB)? BuscarParConBarrera(IReadOnlyList<string> barrios, IEnumerable<BarreraGeografica> barreras)
    {
        var listaBarreras = barreras as ICollection<BarreraGeografica> ?? barreras.ToList();
        for (var i = 0; i < barrios.Count; i++)
        {
            for (var j = i + 1; j < barrios.Count; j++)
            {
                if (HayBarreraEntre(listaBarreras, barrios[i], barrios[j]))
                {
                    return (barrios[i], barrios[j]);
                }
            }
        }

        return null;
    }

    private static string Normalizar(string texto)
    {
        var descompuesto = texto.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        return new string(descompuesto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }
}
