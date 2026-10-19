namespace LFMova.Application.Validators;

/// <summary>
/// Valida que un registro operativo del conductor (inicio y fin de ruta, incidencia con su
/// evidencia, llamada) llegue con una ubicación GPS real: ambas coordenadas presentes y dentro
/// de rango. No decide qué hacer cuando falta (eso lo hace quien la invoca) ni exige nada a los
/// registros históricos anteriores a esta regla, que pueden seguir sin ubicación.
/// </summary>
public static class UbicacionValidador
{
    /// <summary>Indica si las coordenadas existen y son una latitud/longitud geográficamente posibles.</summary>
    public static bool EsValida(double? latitud, double? longitud) =>
        latitud is >= -90 and <= 90 && longitud is >= -180 and <= 180;

    /// <summary>
    /// Lanza <see cref="ArgumentException"/> con un mensaje para el conductor si falta la ubicación
    /// o no es válida, y devuelve las coordenadas ya sin nulos cuando sí lo es.
    /// </summary>
    public static (double Latitud, double Longitud) Exigir(double? latitud, double? longitud, string accion)
    {
        if (!EsValida(latitud, longitud))
        {
            throw new ArgumentException(
                $"No se puede {accion} sin la ubicación del dispositivo. Activa el GPS y concede el permiso de ubicación a la aplicación.");
        }

        return (latitud!.Value, longitud!.Value);
    }
}
