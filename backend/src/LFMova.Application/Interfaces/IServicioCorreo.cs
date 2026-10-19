namespace LFMova.Application.Interfaces;

/// <summary>
/// Define el envío de correos transaccionales (confirmación de cuenta,
/// restablecimiento de contraseña, soporte de rutas). No decide el contenido
/// de negocio del mensaje: eso lo compone quien lo invoca.
/// </summary>
public interface IServicioCorreo
{
    /// <summary>Envía un correo en formato HTML al destinatario indicado.</summary>
    Task EnviarAsync(string destinatarioEmail, string destinatarioNombre, string asunto, string cuerpoHtml);

    /// <summary>Envía un correo en formato HTML con un archivo adjunto al destinatario indicado.</summary>
    Task EnviarConAdjuntoAsync(string destinatarioEmail, string destinatarioNombre, string asunto, string cuerpoHtml, AdjuntoCorreo adjunto);
}

/// <summary>Archivo que acompaña a un correo: su nombre (con extensión) y su contenido.</summary>
public sealed record AdjuntoCorreo(string NombreArchivo, byte[] Contenido);
