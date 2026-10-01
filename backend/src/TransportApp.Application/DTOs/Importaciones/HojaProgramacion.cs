using TransportApp.Domain.Enums;

namespace TransportApp.Application.DTOs.Importaciones;

/// <summary>
/// Contenido leído de una hoja de programación diaria de un conductor: quién
/// transporta, la fecha indicada en la hoja y los grupos de pasajeros por
/// sede y hora. Es el resultado de interpretar el archivo, antes de validarlo
/// contra los datos de la empresa.
/// </summary>
public class HojaProgramacion
{
    /// <summary>Nombre del transportador (conductor) que figura en la hoja.</summary>
    public string Transportador { get; set; } = string.Empty;

    /// <summary>Texto de la fecha de la hoja, por ejemplo "14-15 SEP".</summary>
    public string FechaTexto { get; set; } = string.Empty;

    /// <summary>Grupos de pasajeros: cada uno corresponde a un servicio (tipo + sede + hora).</summary>
    public List<GrupoHoja> Grupos { get; set; } = new();

    /// <summary>Indica que cada fila trae su propia fecha (formato de tabla plana), en cuyo caso no se deduce por el cruce de medianoche.</summary>
    public bool FechasPorFila { get; set; }

    /// <summary>Problemas de formato encontrados al leer el archivo.</summary>
    public List<string> Errores { get; set; } = new();
}

/// <summary>Filas de una misma sección de la hoja que comparten tipo, sede y hora.</summary>
public class GrupoHoja
{
    /// <summary>Título de la sección tal como aparece en la hoja, por ejemplo "ENTRADA PANAMERICANA".</summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Sentido del transporte según el título de la sección.</summary>
    public TipoServicio Tipo { get; set; }

    /// <summary>Nombre de la sede indicado en el título (lo que sigue a ENTRADA/SALIDA).</summary>
    public string NombreSede { get; set; } = string.Empty;

    /// <summary>Hora del servicio (columna ENTRA para entradas, SALE para salidas).</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>Fecha del servicio si la hoja la trae por fila; <c>null</c> si hay que deducirla.</summary>
    public DateOnly? Fecha { get; set; }

    /// <summary>Pasajeros del grupo, en el orden de la hoja.</summary>
    public List<FilaHoja> Filas { get; set; } = new();
}

/// <summary>Una fila de pasajero de la hoja.</summary>
public class FilaHoja
{
    /// <summary>Número de fila en el archivo, para indicar errores con precisión.</summary>
    public int NumeroFila { get; set; }

    /// <summary>Cédula del empleado.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre completo (nombres y apellidos).</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Dirección de recogida o entrega.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Barrio de la dirección.</summary>
    public string Barrio { get; set; } = string.Empty;

    /// <summary>Celular del empleado.</summary>
    public string Celular { get; set; } = string.Empty;
}
