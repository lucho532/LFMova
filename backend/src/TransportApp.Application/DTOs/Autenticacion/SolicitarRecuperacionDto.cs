namespace TransportApp.Application.DTOs.Autenticacion;

/// <summary>Datos para solicitar el envío de un enlace de recuperación de contraseña.</summary>
public class SolicitarRecuperacionDto
{
    /// <summary>Cédula o correo electrónico de la cuenta.</summary>
    public string Identificador { get; set; } = string.Empty;
}
