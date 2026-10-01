using LFMova.Application.DTOs.Vehiculos;

namespace LFMova.Application.Validators;

/// <summary>
/// Valida los datos de entrada de un vehículo (placa, marca, modelo,
/// capacidad y vigencias de SOAT y técnico-mecánica). Solo comprueba que
/// estén presentes y sean coherentes; no decide qué hacer con un documento
/// vencido, porque el SDD no define esa regla.
/// </summary>
public static class VehiculoValidador
{
    /// <summary>Devuelve el mensaje del primer problema encontrado, o <c>null</c> si los datos son válidos.</summary>
    public static string? ObtenerError(CrearVehiculoDto datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Placa))
        {
            return "La placa del vehículo es obligatoria.";
        }

        if (string.IsNullOrWhiteSpace(datos.Marca) || string.IsNullOrWhiteSpace(datos.Modelo))
        {
            return "La marca y el modelo del vehículo son obligatorios.";
        }

        if (!CapacidadValidador.EsValida(datos.Capacidad))
        {
            return "La capacidad del vehículo debe ser mayor que cero.";
        }

        if (datos.VigenciaSoat == default)
        {
            return "La vigencia del SOAT es obligatoria.";
        }

        if (datos.VigenciaTecnomecanica == default)
        {
            return "La vigencia de la técnico-mecánica es obligatoria.";
        }

        return null;
    }
}
