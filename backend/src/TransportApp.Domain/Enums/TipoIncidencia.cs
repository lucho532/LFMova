namespace TransportApp.Domain.Enums;

/// <summary>
/// Representa los tipos de incidencia que un conductor puede registrar sobre
/// un <c>ServicioPasajero</c> durante la ejecución de un servicio.
/// </summary>
public enum TipoIncidencia
{
    /// <summary>El pasajero no contestó al ser contactado.</summary>
    NO_CONTESTA,

    /// <summary>El pasajero no se encontraba en el punto de recogida.</summary>
    NO_SE_ENCUENTRA,

    /// <summary>La dirección de recogida registrada era incorrecta.</summary>
    DIRECCION_INCORRECTA,

    /// <summary>No fue posible recoger al pasajero.</summary>
    NO_SE_PUDO_RECOGER,

    /// <summary>La ubicación de recogida fue modificada respecto a la programada.</summary>
    UBICACION_MODIFICADA,

    /// <summary>Cualquier otra incidencia no cubierta por los tipos anteriores.</summary>
    OTRA
}
