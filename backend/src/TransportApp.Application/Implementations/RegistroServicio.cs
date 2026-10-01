using TransportApp.Application.DTOs.Autenticacion;
using TransportApp.Application.Interfaces;
using TransportApp.Application.Utils;
using TransportApp.Application.Validators;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Domain.Rules;

namespace TransportApp.Application.Implementations;

/// <summary>
/// Implementa el autorregistro de cualquier persona en la plataforma con el
/// rol <c>EMPLEADO</c> sin empresa asignada, y la confirmación de su correo
/// mediante un enlace enviado por correo (reemplaza al mecanismo de código
/// generado por un conductor). No decide autorización: eso se verifica en la
/// capa de Api.
/// </summary>
public class RegistroServicio : IRegistroServicio
{
    /// <summary>Duración de validez de un enlace de confirmación de correo recién generado.</summary>
    private static readonly TimeSpan DuracionValidez = TimeSpan.FromHours(48);

    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IUsuarioRolRepositorio _usuarioRolRepositorio;
    private readonly ITokenVerificacionRepositorio _tokenRepositorio;
    private readonly IHasheadorContrasenas _hasheadorContrasenas;
    private readonly IServicioCorreo _servicioCorreo;
    private readonly OpcionesFrontend _opcionesFrontend;
    private readonly IInvitacionEmpresaServicio _invitacionServicio;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public RegistroServicio(
        IUsuarioRepositorio usuarioRepositorio,
        IUsuarioRolRepositorio usuarioRolRepositorio,
        ITokenVerificacionRepositorio tokenRepositorio,
        IHasheadorContrasenas hasheadorContrasenas,
        IServicioCorreo servicioCorreo,
        OpcionesFrontend opcionesFrontend,
        IInvitacionEmpresaServicio invitacionServicio)
    {
        _usuarioRepositorio = usuarioRepositorio;
        _usuarioRolRepositorio = usuarioRolRepositorio;
        _tokenRepositorio = tokenRepositorio;
        _hasheadorContrasenas = hasheadorContrasenas;
        _servicioCorreo = servicioCorreo;
        _opcionesFrontend = opcionesFrontend;
        _invitacionServicio = invitacionServicio;
    }

    /// <inheritdoc />
    public async Task<ResultadoRegistroDto> RegistrarAsync(RegistrarUsuarioDto datos)
    {
        if (!CedulaValidador.EsValida(datos.Cedula))
        {
            throw new InvalidOperationException("La cédula es obligatoria.");
        }

        var correo = CorreoNormalizador.Normalizar(datos.Correo ?? string.Empty);
        if (!EmailValidador.EsValido(correo))
        {
            throw new InvalidOperationException("El correo electrónico no es válido.");
        }

        if (string.IsNullOrWhiteSpace(datos.NombreCompleto))
        {
            throw new InvalidOperationException("El nombre completo es obligatorio.");
        }

        if (!TelefonoValidador.EsValido(datos.Telefono))
        {
            throw new InvalidOperationException("El teléfono es obligatorio.");
        }

        if (!PasswordValidador.EsValida(datos.Password))
        {
            throw new InvalidOperationException("La contraseña es obligatoria.");
        }

        // Se valida antes de crear nada: una invitación inválida no debe dejar una cuenta a medias.
        var invitacion = string.IsNullOrWhiteSpace(datos.TokenInvitacion)
            ? null
            : await _invitacionServicio.ObtenerParaRegistroAsync(datos.TokenInvitacion, datos.Cedula);

        var existente = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(datos.Cedula);
        // Una cuenta creada por importación de Excel aún no tiene correo ni contraseña: la persona la reclama al registrarse.
        var esCuentaImportada = existente is not null && existente.PasswordHash is null && existente.Email is null;

        if (existente is not null && !esCuentaImportada)
        {
            throw new InvalidOperationException("Ya existe una cuenta registrada con esta cédula.");
        }

        if (await _usuarioRepositorio.ObtenerPorEmailAsync(correo) is not null)
        {
            throw new InvalidOperationException("Ya existe una cuenta registrada con este correo.");
        }

        var usuario = existente ?? new Usuario { Cedula = datos.Cedula, Activo = true };
        usuario.Email = correo;
        usuario.NombreCompleto = datos.NombreCompleto;
        usuario.Telefono = datos.Telefono;
        usuario.PasswordHash = _hasheadorContrasenas.Hashear(datos.Password);
        // Si se registra con el mismo correo al que llegó la invitación, abrir ese enlace ya demostró que el correo es suyo.
        usuario.CorreoConfirmado = invitacion is not null && invitacion.Correo == usuario.Email;

        if (esCuentaImportada)
        {
            await _usuarioRepositorio.GuardarCambiosAsync();
        }
        else
        {
            await _usuarioRepositorio.AgregarAsync(usuario);
            await _usuarioRepositorio.GuardarCambiosAsync();

            await _usuarioRolRepositorio.AgregarAsync(new UsuarioRol
            {
                UsuarioId = usuario.UsuarioId,
                Rol = Rol.EMPLEADO,
                EmpresaId = null,
                Activo = true
            });
            await _usuarioRolRepositorio.GuardarCambiosAsync();
        }

        if (invitacion is not null)
        {
            await _invitacionServicio.AceptarParaUsuarioAsync(invitacion, usuario);
        }

        if (usuario.CorreoConfirmado)
        {
            return new ResultadoRegistroDto { RequiereConfirmarCorreo = false };
        }

        var tokenTextoPlano = TokenVerificacionTexto.Generar(usuario.UsuarioId);
        var ahora = DateTime.UtcNow;

        await _tokenRepositorio.AgregarAsync(new TokenVerificacion
        {
            UsuarioId = usuario.UsuarioId,
            Tipo = TipoTokenVerificacion.CONFIRMACION_CORREO,
            TokenHash = _hasheadorContrasenas.Hashear(tokenTextoPlano),
            FechaGeneracion = ahora,
            FechaExpiracion = ahora.Add(DuracionValidez),
            Utilizado = false
        });
        await _tokenRepositorio.GuardarCambiosAsync();

        var enlace = $"{_opcionesFrontend.UrlBase}/confirmar-correo?token={Uri.EscapeDataString(tokenTextoPlano)}";
        await _servicioCorreo.EnviarAsync(
            usuario.Email!,
            usuario.NombreCompleto,
            "Confirma tu cuenta de LFMova",
            $"<p>Hola {usuario.NombreCompleto},</p>"
                + "<p>Gracias por registrarte en LFMova. Confirma tu cuenta haciendo clic en el siguiente enlace:</p>"
                + $"<p><a href=\"{enlace}\">Confirmar mi cuenta</a></p>"
                + "<p>Este enlace vence en 48 horas. Si tú no creaste esta cuenta, ignora este mensaje.</p>");

        return new ResultadoRegistroDto { RequiereConfirmarCorreo = true };
    }

    /// <inheritdoc />
    public async Task ConfirmarCorreoAsync(ConfirmarCorreoDto datos)
    {
        var usuarioId = TokenVerificacionTexto.ExtraerUsuarioId(datos.Token);
        if (usuarioId is null)
        {
            throw new InvalidOperationException("El enlace de confirmación no es válido.");
        }

        var usuario = await _usuarioRepositorio.ObtenerPorIdAsync(usuarioId.Value);
        if (usuario is null)
        {
            throw new InvalidOperationException("El enlace de confirmación no es válido.");
        }

        if (usuario.CorreoConfirmado)
        {
            throw new InvalidOperationException("Esta cuenta ya fue confirmada.");
        }

        var ahora = DateTime.UtcNow;
        var candidatos = (await _tokenRepositorio.ObtenerNoUtilizadosPorUsuarioAsync(usuario.UsuarioId, TipoTokenVerificacion.CONFIRMACION_CORREO))
            .Where(t => ReglasTokenVerificacion.EsValido(t, ahora))
            .ToList();

        var tokenCoincidente = candidatos.FirstOrDefault(t => _hasheadorContrasenas.Verificar(datos.Token, t.TokenHash));
        if (tokenCoincidente is null)
        {
            throw new InvalidOperationException("El enlace de confirmación no es válido o expiró.");
        }

        tokenCoincidente.Utilizado = true;
        usuario.CorreoConfirmado = true;

        await _tokenRepositorio.GuardarCambiosAsync();
    }
}
