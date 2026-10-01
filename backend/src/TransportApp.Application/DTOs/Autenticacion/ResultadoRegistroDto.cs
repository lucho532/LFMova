namespace TransportApp.Application.DTOs.Autenticacion;

/// <summary>Resultado del autorregistro: indica si la persona todavía debe confirmar su correo antes de iniciar sesión.</summary>
public class ResultadoRegistroDto
{
    /// <summary>
    /// <c>true</c> si se le envió un enlace de confirmación; <c>false</c> si
    /// se registró desde una invitación con el mismo correo al que llegó
    /// (ya quedó confirmado y puede iniciar sesión de inmediato).
    /// </summary>
    public bool RequiereConfirmarCorreo { get; set; }
}
