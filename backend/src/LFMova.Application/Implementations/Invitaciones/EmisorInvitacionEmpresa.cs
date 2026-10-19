using LFMova.Application.DTOs.Autenticacion;
using LFMova.Application.DTOs.Invitaciones;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Application.Validators;
using LFMova.Domain.Entities;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.Invitaciones;

/// <summary>
/// Crea y envía por correo la invitación de una persona a una empresa. Protege los datos de las
/// personas: si la cédula ya tiene cuenta con correo, la invitación va siempre a ese correo (nunca al
/// escrito por el coordinador), y el coordinador no ve si la cuenta existía ni a qué correo se envió.
/// No acepta invitaciones ni asigna roles.
/// </summary>
public class EmisorInvitacionEmpresa
{
    /// <summary>Duración de validez de una invitación recién enviada.</summary>
    public static readonly TimeSpan DuracionValidez = TimeSpan.FromDays(7);

    private readonly IInvitacionEmpresaRepositorio _invitacionRepositorio;
    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IEmpresaRepositorio _empresaRepositorio;
    private readonly IPersonaServicio _personaServicio;
    private readonly IHasheadorContrasenas _hasheadorContrasenas;
    private readonly IServicioCorreo _servicioCorreo;
    private readonly OpcionesFrontend _opcionesFrontend;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public EmisorInvitacionEmpresa(
        IInvitacionEmpresaRepositorio invitacionRepositorio,
        IUsuarioRepositorio usuarioRepositorio,
        IEmpresaRepositorio empresaRepositorio,
        IPersonaServicio personaServicio,
        IHasheadorContrasenas hasheadorContrasenas,
        IServicioCorreo servicioCorreo,
        OpcionesFrontend opcionesFrontend)
    {
        _invitacionRepositorio = invitacionRepositorio;
        _usuarioRepositorio = usuarioRepositorio;
        _empresaRepositorio = empresaRepositorio;
        _personaServicio = personaServicio;
        _hasheadorContrasenas = hasheadorContrasenas;
        _servicioCorreo = servicioCorreo;
        _opcionesFrontend = opcionesFrontend;
    }

    /// <summary>Invita a la persona de esa cédula a la empresa; una invitación nueva reemplaza a la pendiente anterior.</summary>
    public async Task InvitarAsync(int empresaId, CrearInvitacionEmpresaDto datos, int usuarioInvitadorId)
    {
        var cedula = datos.Cedula.Trim();
        var correoEscrito = CorreoNormalizador.Normalizar(datos.Correo);

        if (!CedulaValidador.EsValida(cedula))
        {
            throw new InvalidOperationException("La cédula es obligatoria.");
        }

        if (!EmailValidador.EsValido(correoEscrito))
        {
            throw new InvalidOperationException("El correo electrónico no es válido.");
        }

        var empresa = await _empresaRepositorio.ObtenerPorIdAsync(empresaId)
            ?? throw new InvalidOperationException("La empresa indicada no existe.");
        var invitador = await _usuarioRepositorio.ObtenerPorIdAsync(usuarioInvitadorId)
            ?? throw new InvalidOperationException("El usuario que invita no existe.");

        if (await _personaServicio.EstaRelacionadaConEmpresaAsync(cedula, empresaId))
        {
            throw new InvalidOperationException("Esta persona ya hace parte de la empresa: búscala en la lista.");
        }

        var usuario = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(cedula);
        string destino;
        string nombreDestino;
        if (usuario?.Email is not null)
        {
            // Si la cuenta ya tiene correo, la invitación va a ese correo (nunca al escrito por el
            // coordinador): así solo el verdadero dueño de la cédula puede aceptarla.
            destino = usuario.Email;
            nombreDestino = usuario.NombreCompleto;
        }
        else
        {
            var duenoDelCorreo = await _usuarioRepositorio.ObtenerPorEmailAsync(correoEscrito);
            if (duenoDelCorreo is not null && duenoDelCorreo.Cedula != cedula)
            {
                throw new InvalidOperationException("Ese correo ya pertenece a otra cuenta registrada. Verifica la cédula o el correo.");
            }

            destino = correoEscrito;
            nombreDestino = usuario?.NombreCompleto ?? string.Empty;
        }

        var ahora = DateTime.UtcNow;

        // Una invitación nueva reemplaza a las anteriores todavía pendientes para la misma cédula.
        foreach (var anterior in await _invitacionRepositorio.ObtenerPorEmpresaYCedulaAsync(empresaId, cedula))
        {
            if (ReglasInvitacionEmpresa.PuedeAceptarse(anterior, ahora))
            {
                anterior.FechaExpiracion = ahora;
            }
        }

        var invitacion = new InvitacionEmpresa
        {
            EmpresaId = empresaId,
            Cedula = cedula,
            Correo = destino,
            UsuarioInvitadorId = usuarioInvitadorId,
            TokenHash = string.Empty,
            FechaCreacion = ahora,
            FechaExpiracion = ahora.Add(DuracionValidez)
        };
        await _invitacionRepositorio.AgregarAsync(invitacion);
        await _invitacionRepositorio.GuardarCambiosAsync();

        // El token incluye el identificador de la invitación, que solo existe después de guardarla.
        var tokenTextoPlano = TokenInvitacionTexto.Generar(invitacion.InvitacionEmpresaId);
        invitacion.TokenHash = _hasheadorContrasenas.Hashear(tokenTextoPlano);
        await _invitacionRepositorio.GuardarCambiosAsync();

        var enlace = $"{_opcionesFrontend.UrlBase}/invitacion?token={Uri.EscapeDataString(tokenTextoPlano)}";
        var saludo = string.IsNullOrWhiteSpace(nombreDestino) ? "Hola," : $"Hola {nombreDestino},";
        await _servicioCorreo.EnviarAsync(
            destino,
            nombreDestino,
            $"{invitador.NombreCompleto} te invita a unirte a {empresa.Nombre} en LFMova",
            $"<p>{saludo}</p>"
                + $"<p><strong>{invitador.NombreCompleto}</strong> te invita a formar parte de <strong>{empresa.Nombre}</strong> en LFMova.</p>"
                + $"<p><a href=\"{enlace}\">Ver la invitación</a></p>"
                + "<p>Si todavía no tienes cuenta, podrás crearla desde ese mismo enlace. La invitación vence en 7 días. "
                + "Si no conoces a quien te invita, ignora este mensaje: nadie tendrá acceso a tus datos sin que aceptes.</p>");
    }
}
