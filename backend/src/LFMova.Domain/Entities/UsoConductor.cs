namespace LFMova.Domain.Entities;

/// <summary>
/// Registro de facturación de una ruta finalizada: quién la condujo y cuándo.
/// Se escribe una sola vez, al finalizar la ruta, y guarda una copia de los
/// datos del conductor (cédula, nombre, placa) en vez de enlazarlo: así el
/// cierre mensual sigue contando a un conductor aunque después se elimine su
/// cuenta. No se modifica ni se borra, y no interviene en la operación.
/// </summary>
public class UsoConductor
{
    /// <summary>Identificador del registro.</summary>
    public int UsoConductorId { get; set; }

    /// <summary>Empresa para la que se hizo la ruta (a la que se le cobra).</summary>
    public int EmpresaId { get; set; }

    /// <summary>Ruta finalizada. Hay un único registro por ruta.</summary>
    public int ServicioId { get; set; }

    /// <summary>Día en que se finalizó la ruta, en hora de Colombia: define el mes al que se factura.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Identificador que tenía el conductor (copia, sin relación: el conductor puede ya no existir).</summary>
    public int ConductorId { get; set; }

    /// <summary>Cédula del conductor en el momento de finalizar.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre del conductor en el momento de finalizar.</summary>
    public string NombreConductor { get; set; } = string.Empty;

    /// <summary>Placa del vehículo con el que se hizo la ruta.</summary>
    public string Placa { get; set; } = string.Empty;

    /// <summary>Pasajeros que quedaron recogidos en la ruta.</summary>
    public int PasajerosTransportados { get; set; }

    /// <summary>Empresa a la que pertenece el registro.</summary>
    public Empresa? Empresa { get; set; }
}
