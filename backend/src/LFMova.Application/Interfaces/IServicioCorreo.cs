namespace LFMova.Application.Interfaces;

/// <summary>
/// Define el envío de correos transaccionales (confirmación de cuenta,
/// restablecimiento de contraseña). No decide el contenido de negocio del
/// mensaje: eso lo compone quien lo invoca.
/// </summary>
public interface IServicioCorreo
{
    /// <summary>Envía un correo en formato HTML al destinatario indicado.</summary>
    Task EnviarAsync(string destinatarioEmail, string destinatarioNombre, string asunto, string cuerpoHtml);
}
