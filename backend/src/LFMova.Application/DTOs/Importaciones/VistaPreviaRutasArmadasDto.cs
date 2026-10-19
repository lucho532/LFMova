using LFMova.Domain.Enums;

namespace LFMova.Application.DTOs.Importaciones;

/// <summary>
/// Resultado de validar, sin guardar nada, un Excel con rutas ya armadas: las rutas tal como vienen,
/// con el conductor que corresponde a cada placa, y las advertencias y errores encontrados.
/// </summary>
public class VistaPreviaRutasArmadasDto
{
    /// <summary>Rutas que se crearían, en el orden del archivo.</summary>
    public List<RutaArmadaPreviaDto> Rutas { get; set; } = new();

    /// <summary>Total de pasajeros en todas las rutas.</summary>
    public int TotalPasajeros { get; set; }

    /// <summary>Fecha más temprana que trae el archivo; <c>null</c> si no trae fechas.</summary>
    public DateOnly? FechaOperativaSugerida { get; set; }

    /// <summary>Indica si alguna ruta no trae sede: el coordinador debe elegir una al importar.</summary>
    public bool FaltaSede { get; set; }

    /// <summary>Situaciones que conviene revisar pero no impiden importar.</summary>
    public List<string> Advertencias { get; set; } = new();

    /// <summary>Problemas que impiden importar.</summary>
    public List<string> Errores { get; set; } = new();

    /// <summary>Indica si el archivo se puede importar (no tiene errores).</summary>
    public bool PuedeImportar => Errores.Count == 0 && Rutas.Count > 0;
}

/// <summary>Una ruta de la vista previa de un Excel con rutas armadas.</summary>
public class RutaArmadaPreviaDto
{
    /// <summary>Número de la ruta o texto de la línea que la abre.</summary>
    public string Referencia { get; set; } = string.Empty;

    /// <summary>Entrada o salida.</summary>
    public TipoServicio Tipo { get; set; }

    /// <summary>Hora programada.</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>Fecha de la ruta si el archivo la trae.</summary>
    public DateOnly? Fecha { get; set; }

    /// <summary>Sede escrita en el archivo, vacía si no trae.</summary>
    public string SedeEnHoja { get; set; } = string.Empty;

    /// <summary>Sede de la empresa que coincide con la del archivo, o <c>null</c>.</summary>
    public string? SedeEncontrada { get; set; }

    /// <summary>Placa que trae el archivo para la ruta.</summary>
    public string Placa { get; set; } = string.Empty;

    /// <summary>Nombre del conductor de la unidad que corresponde a la placa, o <c>null</c> si la ruta quedará sin conductor.</summary>
    public string? Conductor { get; set; }

    /// <summary>Número interno que la empresa le asignó al vehículo de la unidad, o <c>null</c> si no tiene.</summary>
    public string? NumeroInterno { get; set; }

    /// <summary>Capacidad del vehículo, para comparar con los pasajeros; <c>null</c> si no se identificó.</summary>
    public int? Capacidad { get; set; }

    /// <summary>Pasajeros en el orden del archivo.</summary>
    public List<PasajeroPreviaDto> Pasajeros { get; set; } = new();
}
