using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TransportApp.Application.Interfaces;

namespace TransportApp.Infrastructure.Correo;

/// <summary>
/// Implementa <see cref="IServicioCorreo"/> mediante la API transaccional de
/// Brevo (<c>POST https://api.brevo.com/v3/smtp/email</c>). No decide el
/// contenido de negocio del mensaje ni reintenta envíos fallidos: propaga la
/// excepción para que la capa de aplicación decida cómo tratarla.
/// </summary>
public class ServicioCorreoBrevo : IServicioCorreo
{
    private const string RutaEnvio = "v3/smtp/email";

    private readonly HttpClient _cliente;
    private readonly OpcionesBrevo _opciones;

    /// <summary>Crea el servicio con el cliente HTTP configurado y las opciones de Brevo.</summary>
    public ServicioCorreoBrevo(HttpClient cliente, IOptions<OpcionesBrevo> opciones)
    {
        _opciones = opciones.Value;

        cliente.BaseAddress = new Uri("https://api.brevo.com/");
        cliente.DefaultRequestHeaders.Remove("api-key");
        cliente.DefaultRequestHeaders.Add("api-key", _opciones.ApiKey);
        _cliente = cliente;
    }

    /// <inheritdoc />
    public async Task EnviarAsync(string destinatarioEmail, string destinatarioNombre, string asunto, string cuerpoHtml)
    {
        var solicitud = new SolicitudEnvioBrevo
        {
            Sender = new ContactoBrevo { Email = _opciones.RemitenteEmail, Name = _opciones.RemitenteNombre },
            // Brevo rechaza un destinatario con el nombre vacío ("name is missing in to"), pero acepta que no
            // venga: pasa, por ejemplo, al invitar a alguien que todavía no tiene cuenta (no se sabe su nombre).
            To = [new ContactoBrevo { Email = destinatarioEmail, Name = string.IsNullOrWhiteSpace(destinatarioNombre) ? null : destinatarioNombre }],
            Subject = asunto,
            HtmlContent = cuerpoHtml
        };

        var respuesta = await _cliente.PostAsJsonAsync(RutaEnvio, solicitud);
        if (!respuesta.IsSuccessStatusCode)
        {
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"No se pudo enviar el correo mediante Brevo ({(int)respuesta.StatusCode}): {cuerpo}");
        }
    }

    private class SolicitudEnvioBrevo
    {
        [JsonPropertyName("sender")]
        public ContactoBrevo Sender { get; set; } = new();

        [JsonPropertyName("to")]
        public List<ContactoBrevo> To { get; set; } = [];

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("htmlContent")]
        public string HtmlContent { get; set; } = string.Empty;
    }

    private class ContactoBrevo
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }
    }
}
