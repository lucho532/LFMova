using TransportApp.Domain.Enums;

namespace TransportApp.Application.Validators;

/// <summary>
/// Valida que un <c>TipoServicio</c> recibido desde un DTO corresponda a un
/// valor definido del enum (protege contra valores fuera de rango que el
/// enlace de modelo de ASP.NET Core no rechaza automáticamente).
/// </summary>
public static class TipoServicioValidador
{
    /// <summary>Indica si el tipo de servicio es un valor definido.</summary>
    public static bool EsValido(TipoServicio tipo) => Enum.IsDefined(tipo);
}
