namespace LFMova.Application.DTOs.Facturacion;

/// <summary>
/// Fila del resumen de facturación del administrador: lo que una empresa usó
/// la plataforma en un mes. No incluye precios: solo las cantidades.
/// </summary>
public class ResumenFacturacionEmpresaDto
{
    /// <summary>Identificador de la empresa.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Nombre de la empresa.</summary>
    public string NombreEmpresa { get; set; } = string.Empty;

    /// <summary>Conductores vinculados hoy a la empresa (dato informativo, no se cobra por él).</summary>
    public int ConductoresVinculados { get; set; }

    /// <summary>Conductores distintos que finalizaron al menos una ruta en el mes: la base del cobro.</summary>
    public int ConductoresActivos { get; set; }

    /// <summary>Rutas finalizadas en el mes.</summary>
    public int RutasFinalizadas { get; set; }

    /// <summary>Pasajeros recogidos en esas rutas.</summary>
    public int PasajerosTransportados { get; set; }

    /// <summary>Indica si el mes ya está cerrado para esta empresa (los totales quedaron fijos).</summary>
    public bool Cerrado { get; set; }

    /// <summary>Instante (UTC) del cierre, si lo hay.</summary>
    public DateTime? FechaCierre { get; set; }
}

/// <summary>Un conductor que finalizó rutas para la empresa en el mes, con el soporte de su actividad.</summary>
public class ConductorFacturableDto
{
    /// <summary>Cédula del conductor (como estaba al finalizar sus rutas).</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre del conductor.</summary>
    public string NombreConductor { get; set; } = string.Empty;

    /// <summary>Placas de los vehículos con los que hizo las rutas.</summary>
    public List<string> Placas { get; set; } = new();

    /// <summary>Rutas que finalizó en el mes.</summary>
    public int RutasFinalizadas { get; set; }

    /// <summary>Pasajeros que recogió en esas rutas.</summary>
    public int PasajerosTransportados { get; set; }

    /// <summary>Día de su primera ruta finalizada del mes.</summary>
    public DateOnly PrimeraRuta { get; set; }

    /// <summary>Día de su última ruta finalizada del mes.</summary>
    public DateOnly UltimaRuta { get; set; }

    /// <summary>Indica si su cuenta de conductor ya no existe (fue eliminada después de hacer las rutas).</summary>
    public bool Eliminado { get; set; }
}

/// <summary>Detalle de facturación de una empresa en un mes: sus totales y los conductores que los explican.</summary>
public class DetalleFacturacionDto
{
    /// <summary>Totales de la empresa en el mes.</summary>
    public ResumenFacturacionEmpresaDto Resumen { get; set; } = new();

    /// <summary>Año consultado.</summary>
    public int Anio { get; set; }

    /// <summary>Mes consultado (1 a 12).</summary>
    public int Mes { get; set; }

    /// <summary>Conductores que finalizaron rutas en el mes, ordenados por nombre.</summary>
    public List<ConductorFacturableDto> Conductores { get; set; } = new();
}
