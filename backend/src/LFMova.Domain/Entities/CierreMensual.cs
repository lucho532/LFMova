namespace LFMova.Domain.Entities;

/// <summary>
/// Cierre de facturación de una empresa para un mes: deja fijos los totales
/// con los que se cobra (conductores que finalizaron rutas, rutas y pasajeros).
/// Solo existe uno por empresa y mes, y una vez creado no se recalcula.
/// </summary>
public class CierreMensual
{
    /// <summary>Identificador del cierre.</summary>
    public int CierreMensualId { get; set; }

    /// <summary>Empresa a la que corresponde el cierre.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Año del mes cerrado.</summary>
    public int Anio { get; set; }

    /// <summary>Mes cerrado (1 a 12).</summary>
    public int Mes { get; set; }

    /// <summary>Instante (UTC) en que se hizo el cierre.</summary>
    public DateTime FechaCierre { get; set; }

    /// <summary>Conductores distintos que finalizaron al menos una ruta en el mes: la base del cobro.</summary>
    public int ConductoresActivos { get; set; }

    /// <summary>Rutas finalizadas en el mes.</summary>
    public int RutasFinalizadas { get; set; }

    /// <summary>Pasajeros recogidos en esas rutas.</summary>
    public int PasajerosTransportados { get; set; }

    /// <summary>Empresa a la que corresponde el cierre.</summary>
    public Empresa? Empresa { get; set; }
}
