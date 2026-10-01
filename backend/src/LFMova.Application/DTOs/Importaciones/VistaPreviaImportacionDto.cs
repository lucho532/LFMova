using LFMova.Domain.Enums;

namespace LFMova.Application.DTOs.Importaciones;

/// <summary>
/// Resultado de validar un archivo de programación sin guardar nada: qué
/// servicios y pasajeros contiene, qué pasaría con cada empleado, y las
/// advertencias y errores encontrados. Si hay errores, no se puede importar.
/// </summary>
public class VistaPreviaImportacionDto
{
    /// <summary>Transportador (conductor) que figura en la hoja.</summary>
    public string Transportador { get; set; } = string.Empty;

    /// <summary>Texto de la fecha de la hoja, por ejemplo "14-15 SEP".</summary>
    public string FechaTexto { get; set; } = string.Empty;

    /// <summary>Indica si la fecha de la hoja abarca dos días (turno que cruza la medianoche).</summary>
    public bool CruzaMedianoche { get; set; }

    /// <summary>Fecha operativa sugerida cuando la hoja trae fechas por fila (la más temprana); <c>null</c> en otro caso.</summary>
    public DateOnly? FechaOperativaSugerida { get; set; }

    /// <summary>Servicios que se crearían, uno por cada grupo de sede + hora.</summary>
    public List<ServicioPreviaDto> Servicios { get; set; } = new();

    /// <summary>Total de pasajeros en todos los servicios.</summary>
    public int TotalPasajeros { get; set; }

    /// <summary>Cantidad de empleados cuya cédula todavía no existe en la plataforma.</summary>
    public int EmpleadosNuevos { get; set; }

    /// <summary>Situaciones que conviene revisar pero no impiden importar.</summary>
    public List<string> Advertencias { get; set; } = new();

    /// <summary>Problemas que impiden importar.</summary>
    public List<string> Errores { get; set; } = new();

    /// <summary>
    /// Barrios del archivo que no coinciden con el de ninguna Zona activa de la empresa (comparación
    /// exacta de texto, sin mayúsculas ni tildes): esas personas quedan en "sin zona asignada" al
    /// repartir. Se muestran aparte de <see cref="Advertencias"/> para que el coordinador pueda
    /// resolverlos con una acción concreta (asignar el barrio a una zona existente o crear una nueva),
    /// no solo como un aviso de lectura.
    /// </summary>
    public List<string> BarriosSinZona { get; set; } = new();

    /// <summary>Indica si el archivo se puede importar (no tiene errores).</summary>
    public bool PuedeImportar => Errores.Count == 0;
}

/// <summary>Servicio que se crearía a partir de un grupo de la hoja.</summary>
public class ServicioPreviaDto
{
    /// <summary>Sentido del transporte.</summary>
    public TipoServicio Tipo { get; set; }

    /// <summary>Nombre de la sede indicado en la hoja.</summary>
    public string SedeEnHoja { get; set; } = string.Empty;

    /// <summary>Sede de la empresa con la que coincide, o <c>null</c> si no existe.</summary>
    public string? SedeEncontrada { get; set; }

    /// <summary>Id de la sede encontrada, o <c>null</c> si no existe todavía. Permite emparejar este grupo con el servicio real ya creado para esa ruta.</summary>
    public int? SedeId { get; set; }

    /// <summary>Hora del servicio.</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>Fecha del servicio si la hoja la indica por fila.</summary>
    public DateOnly? Fecha { get; set; }

    /// <summary>Pasajeros del servicio.</summary>
    public List<PasajeroPreviaDto> Pasajeros { get; set; } = new();
}

/// <summary>Pasajero de un servicio de la vista previa.</summary>
public class PasajeroPreviaDto
{
    /// <summary>Cédula del empleado.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre completo.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Dirección de recogida.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Barrio.</summary>
    public string Barrio { get; set; } = string.Empty;

    /// <summary>Celular.</summary>
    public string Celular { get; set; } = string.Empty;

    /// <summary>
    /// Situación del empleado: <c>NUEVO</c> (no existe la cédula),
    /// <c>CUENTA_REGISTRADA</c> (la persona ya se registró y se vinculará a la
    /// empresa), <c>EXISTENTE</c> (ya es empleado de esta empresa) o
    /// <c>CAMBIA_DE_EMPRESA</c> (era empleado de otra empresa y pasará a esta).
    /// </summary>
    public string Situacion { get; set; } = string.Empty;
}
