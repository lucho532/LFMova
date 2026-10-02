using System.Collections.Concurrent;
using LFMova.Application.Interfaces;

namespace LFMova.IntegrationTests.Fixtures;

/// <summary>
/// Sustituye al servicio real de correo (Brevo) en las pruebas de
/// integración: no realiza ninguna llamada de red y conserva los mensajes
/// "enviados" para que las pruebas puedan extraer los enlaces con token.
/// </summary>
public class ServicioCorreoFalso : IServicioCorreo
{
    private readonly ConcurrentQueue<(string Destino, string Asunto, string Cuerpo)> _enviados = new();
    private readonly ConcurrentQueue<(string Destino, string Asunto, AdjuntoCorreo Adjunto)> _adjuntos = new();

    /// <inheritdoc />
    public Task EnviarAsync(string destinatarioEmail, string destinatarioNombre, string asunto, string cuerpoHtml)
    {
        _enviados.Enqueue((destinatarioEmail, asunto, cuerpoHtml));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task EnviarConAdjuntoAsync(string destinatarioEmail, string destinatarioNombre, string asunto, string cuerpoHtml, AdjuntoCorreo adjunto)
    {
        _adjuntos.Enqueue((destinatarioEmail, asunto, adjunto));
        return EnviarAsync(destinatarioEmail, destinatarioNombre, asunto, cuerpoHtml);
    }

    /// <summary>Obtiene los adjuntos enviados al destinatario indicado, del más antiguo al más reciente.</summary>
    public List<(string Asunto, AdjuntoCorreo Adjunto)> ObtenerAdjuntos(string destinatarioEmail)
        => _adjuntos.Where(a => a.Destino == destinatarioEmail).Select(a => (a.Asunto, a.Adjunto)).ToList();

    /// <summary>Obtiene el token del enlace del último correo enviado al destinatario indicado.</summary>
    public string ObtenerUltimoToken(string destinatarioEmail)
    {
        var cuerpo = _enviados.Where(m => m.Destino == destinatarioEmail).Last().Cuerpo;
        var inicio = cuerpo.IndexOf("token=", StringComparison.Ordinal) + "token=".Length;
        var fin = cuerpo.IndexOf('"', inicio);
        return Uri.UnescapeDataString(cuerpo[inicio..fin]);
    }
}
