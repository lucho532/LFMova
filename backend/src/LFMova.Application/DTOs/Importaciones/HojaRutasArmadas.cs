using LFMova.Domain.Enums;

namespace LFMova.Application.DTOs.Importaciones;

/// <summary>
/// Contenido leído de un Excel con rutas ya armadas: cada ruta tal como la separó el archivo, antes de
/// validarla contra los datos de la empresa. No decide nada: solo refleja lo que trae el archivo.
/// </summary>
public class HojaRutasArmadas
{
    /// <summary>Rutas en el orden en que aparecen por primera vez en el archivo.</summary>
    public List<RutaArmadaHoja> Rutas { get; set; } = new();

    /// <summary>Avisos de la lectura que no impiden importar (por ejemplo, nombres con caracteres dañados que no se pudieron corregir).</summary>
    public List<string> Advertencias { get; set; } = new();

    /// <summary>Problemas del archivo que impiden importar.</summary>
    public List<string> Errores { get; set; } = new();
}

/// <summary>Una ruta del archivo: sus datos generales y sus pasajeros en el orden de las filas.</summary>
public class RutaArmadaHoja
{
    /// <summary>Cómo identifica el archivo a la ruta: su número o el texto de la línea que la abre.</summary>
    public string Referencia { get; set; } = string.Empty;

    /// <summary>Entrada o salida.</summary>
    public TipoServicio Tipo { get; set; }

    /// <summary>Hora programada de la ruta.</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>Fecha de la ruta si el archivo la trae; <c>null</c> para usar la fecha operativa elegida al importar.</summary>
    public DateOnly? Fecha { get; set; }

    /// <summary>Nombre de la sede si el archivo la trae; vacío si debe elegirla el coordinador.</summary>
    public string NombreSede { get; set; } = string.Empty;

    /// <summary>Placa (o identificador del vehículo) que trae el archivo para la ruta; vacío si no trae.</summary>
    public string Placa { get; set; } = string.Empty;

    /// <summary>Pasajeros en el orden exacto de las filas del archivo.</summary>
    public List<FilaHoja> Filas { get; set; } = new();
}
