namespace LFMova.Application.Validators;

/// <summary>
/// Valida la capacidad de pasajeros de un vehículo.
/// </summary>
public static class CapacidadValidador
{
    /// <summary>Indica si la capacidad es válida: debe ser un número positivo.</summary>
    public static bool EsValida(int capacidad) => capacidad > 0;
}
