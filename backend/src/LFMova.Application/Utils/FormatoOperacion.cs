using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.Application.Utils;

/// <summary>
/// Da formato legible a datos de la operación en textos para las personas
/// (notificaciones), en lugar de códigos internos: nombres con mayúscula
/// inicial y horas de reloj de Colombia en formato de 12 horas. No decide
/// qué se notifica ni a quién.
/// </summary>
public static class FormatoOperacion
{
    /// <summary>Nombre con cada palabra en mayúscula inicial ("andres felipe" → "Andres Felipe").</summary>
    public static string NombrePropio(string nombre)
        => string.Join(' ', nombre
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(palabra => char.ToUpperInvariant(palabra[0]) + palabra[1..].ToLowerInvariant()));

    /// <summary>Hora en formato de 12 horas: "01:00 a. m.", "06:30 p. m.".</summary>
    public static string Hora(TimeOnly hora)
    {
        var hora12 = hora.Hour % 12 == 0 ? 12 : hora.Hour % 12;
        return $"{hora12:00}:{hora.Minute:00} {(hora.Hour < 12 ? "a. m." : "p. m.")}";
    }

    /// <summary>Hora con su artículo, como se dice en español: "la 01:00 a. m.", "las 06:00 a. m.".</summary>
    public static string HoraConArticulo(TimeOnly hora)
        => $"{(hora.Hour % 12 == 1 ? "la" : "las")} {Hora(hora)}";

    /// <summary>
    /// Ruta descrita para una persona, en lugar de su número interno: "ruta de entrada de la 01:00 a. m. a
    /// La Patria (15/09/2026)" o "ruta de salida de las 03:00 p. m. desde La Patria (15/09/2026)".
    /// </summary>
    public static string DescribirRuta(TipoServicio tipo, TimeOnly hora, DateOnly fecha, string? sede)
    {
        var esEntrada = tipo == TipoServicio.ENTRADA;
        var lugar = string.IsNullOrWhiteSpace(sede) ? string.Empty : $" {(esEntrada ? "a" : "desde")} {sede}";
        return $"ruta de {(esEntrada ? "entrada" : "salida")} de {HoraConArticulo(hora)}{lugar} ({fecha:dd/MM/yyyy})";
    }

    /// <summary>Igual que la otra sobrecarga, a partir del servicio (usa su sede si está cargada).</summary>
    public static string DescribirRuta(Servicio servicio)
        => DescribirRuta(servicio.Tipo, servicio.HoraProgramada, servicio.Fecha, servicio.Sede?.Nombre);
}
