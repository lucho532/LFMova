namespace TransportApp.Application.Validators;

/// <summary>
/// Valida el formato básico de una fecha programada (de una
/// <c>ProgramacionTransporte</c> o de un <c>Servicio</c>).
/// </summary>
public static class FechaProgramacionValidador
{
    /// <summary>Indica si la fecha tiene un valor válido (fue efectivamente establecida).</summary>
    public static bool EsValida(DateOnly fecha) => fecha != default;
}
