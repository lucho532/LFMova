using TransportApp.Application.DTOs.Autenticacion;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Utils;
using TransportApp.Application.Validators;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Domain.Rules;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa la recuperación de contraseña mediante un enlace enviado por
/// correo. No decide autorización: eso se verifica en la capa de Api.
/// </summary>
public class RecuperacionContrasenaServicio : IRecuperacionContrasenaServicio
{
    /// <summary>Duración de validez de un enlace de restablecimiento recién generado.</summary>
    private static readonly TimeSpan DuracionValidez = TimeSpan.FromHours(2);

    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly ITokenVerificacionRepositorio _tokenRepositorio;
    private readonly IHasheadorContrasenas _hasheadorContrasenas;
    private readonly IServicioCorreo _servicioCorreo;
    private readonly OpcionesFrontend _opcionesFrontend;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public RecuperacionContrasenaServicio(
        IUsuarioRepositorio usuarioRepositorio,
        ITokenVerificacionRepositorio tokenRepositorio,
        IHasheadorContrasenas hasheadorContrasenas,
        IServicioCorreo servicioCorreo,
        OpcionesFrontend opcionesFrontend)
    {
        _usuarioRepositorio = usuarioRepositorio;
        _tokenRepositorio = tokenRepositorio;
        _hasheadorContrasenas = hasheadorContrasenas;
        _servicioCorreo = servicioCorreo;
        _opcionesFrontend = opcionesFrontend;
    }

    /// <inheritdoc />
    public async Task SolicitarAsync(SolicitarRecuperacionDto datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Identificador))
        {
            throw new InvalidOperationException("La cédula o el correo son obligatorios.");
        }

        var usuario = await _usuarioRepositorio.ObtenerPorIdentificadorConRolesAsync(datos.Identificador);
        if (usuario is null || usuario.Email is null)
        {
            // No revela si la cuenta existe: si no hay a quién enviar, simplemente no se envía nada.
            return;
        }

        var enlace = await GenerarEnlaceAsync(usuario);
        await _servicioCorreo.EnviarAsync(
            usuario.Email,
            usuario.NombreCompleto,
            "Restablece tu contraseña de TransportApp",
            $"<p>Hola {usuario.NombreCompleto},</p>"
                + "<p>Restablece tu contraseña haciendo clic en el siguiente enlace:</p>"
                + $"<p><a href=\"{enlace}\">Restablecer mi contraseña</a></p>"
                + "<p>Este enlace vence en 2 horas. Si tú no solicitaste este cambio, ignora este mensaje.</p>");
    }

    /// <inheritdoc />
    public async Task EnviarInvitacionAsync(Usuario usuarioInvitado, string contextoInvitacion)
    {
        if (usuarioInvitado.Email is null)
        {
            throw new InvalidOperationException("El usuario invitado no tiene un correo registrado.");
        }

        var enlace = await GenerarEnlaceAsync(usuarioInvitado);
        await _servicioCorreo.EnviarAsync(
            usuarioInvitado.Email,
            usuarioInvitado.NombreCompleto,
            "Te invitaron a TransportApp",
            $"<p>Hola {usuarioInvitado.NombreCompleto},</p>"
                + $"<p>Fuiste invitado {contextoInvitacion} en TransportApp. Establece tu contraseña haciendo clic en el siguiente enlace:</p>"
                + $"<p><a href=\"{enlace}\">Establecer mi contraseña</a></p>"
                + "<p>Este enlace vence en 48 horas.</p>");
    }

    private async Task<string> GenerarEnlaceAsync(Usuario usuario)
    {
        var tokenTextoPlano = TokenVerificacionTexto.Generar(usuario.UsuarioId);
        var ahora = DateTime.UtcNow;

        await _tokenRepositorio.AgregarAsync(new TokenVerificacion
        {
            UsuarioId = usuario.UsuarioId,
            Tipo = TipoTokenVerificacion.ESTABLECER_CONTRASENA,
            TokenHash = _hasheadorContrasenas.Hashear(tokenTextoPlano),
            FechaGeneracion = ahora,
            FechaExpiracion = ahora.Add(DuracionValidez),
            Utilizado = false
        });
        await _tokenRepositorio.GuardarCambiosAsync();

        return $"{_opcionesFrontend.UrlBase}/restablecer-contrasena?token={Uri.EscapeDataString(tokenTextoPlano)}";
    }

    /// <inheritdoc />
    public async Task RestablecerAsync(RestablecerContrasenaDto datos)
    {
        if (datos.NuevaContrasena != datos.ConfirmacionContrasena)
        {
            throw new InvalidOperationException("Las contraseñas no coinciden.");
        }

        if (!PasswordValidador.EsValida(datos.NuevaContrasena))
        {
            throw new InvalidOperationException("La contraseña es obligatoria.");
        }

        var usuarioId = TokenVerificacionTexto.ExtraerUsuarioId(datos.Token);
        if (usuarioId is null)
        {
            throw new InvalidOperationException("El enlace no es válido.");
        }

        var usuario = await _usuarioRepositorio.ObtenerPorIdAsync(usuarioId.Value);
        if (usuario is null)
        {
            throw new InvalidOperationException("El enlace no es válido.");
        }

        var ahora = DateTime.UtcNow;
        var candidatos = (await _tokenRepositorio.ObtenerNoUtilizadosPorUsuarioAsync(usuario.UsuarioId, TipoTokenVerificacion.ESTABLECER_CONTRASENA))
            .Where(t => ReglasTokenVerificacion.EsValido(t, ahora))
            .ToList();

        var tokenCoincidente = candidatos.FirstOrDefault(t => _hasheadorContrasenas.Verificar(datos.Token, t.TokenHash));
        if (tokenCoincidente is null)
        {
            throw new InvalidOperationException("El enlace no es válido o expiró.");
        }

        tokenCoincidente.Utilizado = true;
        usuario.PasswordHash = _hasheadorContrasenas.Hashear(datos.NuevaContrasena);
        usuario.CorreoConfirmado = true;

        await _tokenRepositorio.GuardarCambiosAsync();
    }
}
