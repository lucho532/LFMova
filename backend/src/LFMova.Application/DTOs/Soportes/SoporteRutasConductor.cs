using LFMova.Domain.Enums;

namespace LFMova.Application.DTOs.Soportes;

/// <summary>
/// Contenido del soporte que se le envía por correo a un conductor: sus
/// propias rutas publicadas de una jornada, con los pasajeros de cada una.
/// Es una copia para consultar si la aplicación no está disponible; nunca
/// incluye rutas ni pasajeros de otros conductores.
/// </summary>
public class SoporteRutasConductor
{
    /// <summary>Nombre del conductor dueño del soporte.</summary>
    public string NombreConductor { get; set; } = string.Empty;

    /// <summary>Nombre de la empresa para la que hace las rutas.</summary>
    public string NombreEmpresa { get; set; } = string.Empty;

    /// <summary>Fecha operativa de la jornada.</summary>
    public DateOnly FechaOperativa { get; set; }

    /// <summary>Rutas del conductor, en el orden en que ocurren.</summary>
    public List<RutaSoporte> Rutas { get; set; } = new();
}

/// <summary>Una ruta dentro del soporte: su sentido, sede, fecha y hora, y sus pasajeros.</summary>
public class RutaSoporte
{
    /// <summary>Sentido del transporte (entrada o salida).</summary>
    public TipoServicio Tipo { get; set; }

    /// <summary>Nombre de la sede de la ruta.</summary>
    public string NombreSede { get; set; } = string.Empty;

    /// <summary>Fecha de la ruta (hora de Colombia).</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora programada de la ruta (hora de Colombia).</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>Pasajeros de la ruta, en su orden de recogida.</summary>
    public List<PasajeroSoporte> Pasajeros { get; set; } = new();
}

/// <summary>Un pasajero dentro de una ruta del soporte, con lo necesario para recogerlo.</summary>
public class PasajeroSoporte
{
    /// <summary>Orden de recogida.</summary>
    public int Orden { get; set; }

    /// <summary>Nombre completo del pasajero.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Dirección de recogida utilizada para esa ruta.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Barrio de la dirección.</summary>
    public string Barrio { get; set; } = string.Empty;
}
