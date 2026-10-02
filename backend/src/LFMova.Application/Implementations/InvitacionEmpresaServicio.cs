using LFMova.Application.DTOs.Invitaciones;
using LFMova.Application.Implementations.Invitaciones;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa la invitación por correo a una empresa. Protege los datos de
/// las personas: el coordinador nunca ve si la cédula tenía cuenta ni a qué
/// correo se envió, y la invitación siempre llega al correo de la cuenta
/// cuando ya existe. Al aceptarse, la persona queda como empleado de la
/// empresa sin dirección ni barrio (no es pasajera: va a ser coordinador o
/// conductor). No mueve a quien ya es empleado de otra empresa: ese cambio
/// solo ocurre por importación de Excel (ver <c>AGENTS.md</c> §15). No
/// decide autorización por rol: eso se verifica en la capa de Api.
/// </summary>
public class InvitacionEmpresaServicio : IInvitacionEmpresaServicio
{
    private const string MensajeTokenInvalido = "El enlace de invitación no es válido.";

    private readonly IInvitacionEmpresaRepositorio _invitacionRepositorio;
    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IUsuarioRolRepositorio _usuarioRolRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly IEmpresaRepositorio _empresaRepositorio;
    private readonly INotificacionServicio _notificacionServicio;
    private readonly IHasheadorContrasenas _hasheadorContrasenas;
    private readonly EmisorInvitacionEmpresa _emisor;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public InvitacionEmpresaServicio(
        IInvitacionEmpresaRepositorio invitacionRepositorio,
        IUsuarioRepositorio usuarioRepositorio,
        IUsuarioRolRepositorio usuarioRolRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IEmpresaRepositorio empresaRepositorio,
        INotificacionServicio notificacionServicio,
        IHasheadorContrasenas hasheadorContrasenas,
        EmisorInvitacionEmpresa emisor)
    {
        _invitacionRepositorio = invitacionRepositorio;
        _usuarioRepositorio = usuarioRepositorio;
        _usuarioRolRepositorio = usuarioRolRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _empresaRepositorio = empresaRepositorio;
        _notificacionServicio = notificacionServicio;
        _hasheadorContrasenas = hasheadorContrasenas;
        _emisor = emisor;
    }

    /// <inheritdoc />
    public Task InvitarAsync(int empresaId, CrearInvitacionEmpresaDto datos, int usuarioInvitadorId)
        => _emisor.InvitarAsync(empresaId, datos, usuarioInvitadorId);

    /// <inheritdoc />
    public async Task<List<InvitacionEmpresaDto>> ObtenerPorEmpresaAsync(int empresaId)
    {
        var ahora = DateTime.UtcNow;
        return (await _invitacionRepositorio.ObtenerPorEmpresaAsync(empresaId))
            .Select(i => new InvitacionEmpresaDto
            {
                InvitacionEmpresaId = i.InvitacionEmpresaId,
                Cedula = i.Cedula,
                NombreCompleto = i.FechaAceptacion is null ? null : i.UsuarioAceptante?.NombreCompleto,
                Estado = ReglasInvitacionEmpresa.Estado(i, ahora),
                FechaCreacion = i.FechaCreacion,
                FechaExpiracion = i.FechaExpiracion,
                FechaAceptacion = i.FechaAceptacion
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<DetalleInvitacionDto> ObtenerDetalleAsync(string token)
    {
        var invitacion = await ObtenerPorTokenAsync(token);
        var usuario = await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(invitacion.Cedula);
        var empresa = invitacion.Empresa ?? await _empresaRepositorio.ObtenerPorIdAsync(invitacion.EmpresaId);
        var invitador = invitacion.UsuarioInvitador ?? await _usuarioRepositorio.ObtenerPorIdAsync(invitacion.UsuarioInvitadorId);

        return new DetalleInvitacionDto
        {
            NombreEmpresa = empresa?.Nombre ?? string.Empty,
            NombreInvitador = invitador?.NombreCompleto ?? string.Empty,
            Cedula = invitacion.Cedula,
            Correo = invitacion.Correo,
            // Una cuenta creada por importación de Excel (sin contraseña) también se completa desde el registro.
            RequiereRegistro = usuario?.PasswordHash is null,
            Estado = ReglasInvitacionEmpresa.Estado(invitacion, DateTime.UtcNow),
            FechaExpiracion = invitacion.FechaExpiracion
        };
    }

    /// <inheritdoc />
    public async Task AceptarAsync(string token, int usuarioId)
    {
        var invitacion = await ObtenerPorTokenAsync(token);
        AsegurarQuePuedeAceptarse(invitacion);

        var usuario = await _usuarioRepositorio.ObtenerPorIdAsync(usuarioId)
            ?? throw new InvalidOperationException("El usuario autenticado no existe.");
        if (usuario.Cedula != invitacion.Cedula)
        {
            throw new InvalidOperationException("Esta invitación es para otra persona. Inicia sesión con la cuenta de la cédula invitada.");
        }

        await AceptarParaUsuarioAsync(invitacion, usuario);
    }

    /// <inheritdoc />
    public async Task<InvitacionEmpresa> ObtenerParaRegistroAsync(string token, string cedula)
    {
        var invitacion = await ObtenerPorTokenAsync(token);
        AsegurarQuePuedeAceptarse(invitacion);

        if (invitacion.Cedula != cedula.Trim())
        {
            throw new InvalidOperationException("Esta invitación es para otra cédula.");
        }

        return invitacion;
    }

    /// <inheritdoc />
    public async Task AceptarParaUsuarioAsync(InvitacionEmpresa invitacion, Usuario usuario)
    {
        var empleado = await _empleadoRepositorio.ObtenerPorUsuarioIdAsync(usuario.UsuarioId);
        if (empleado is null)
        {
            // Queda como empleado de la empresa sin dirección ni barrio: no es pasajera, va a ser
            // coordinador o conductor. Si más adelante se importa como pasajera, la importación los completa.
            await _empleadoRepositorio.AgregarAsync(new Empleado
            {
                UsuarioId = usuario.UsuarioId,
                EmpresaId = invitacion.EmpresaId,
                NombreCompleto = usuario.NombreCompleto,
                Telefono = usuario.Telefono,
                Direccion = string.Empty,
                Barrio = string.Empty,
                Activo = true
            });

            var rolEmpleado = (await _usuarioRepositorio.ObtenerPorCedulaConRolesAsync(usuario.Cedula))?
                .UsuarioRoles.FirstOrDefault(r => r.Rol == Rol.EMPLEADO);
            if (rolEmpleado is null)
            {
                await _usuarioRolRepositorio.AgregarAsync(new UsuarioRol { UsuarioId = usuario.UsuarioId, Rol = Rol.EMPLEADO, EmpresaId = invitacion.EmpresaId, Activo = true });
            }
            else
            {
                rolEmpleado.EmpresaId = invitacion.EmpresaId;
                rolEmpleado.Activo = true;
            }
        }

        invitacion.FechaAceptacion = DateTime.UtcNow;
        invitacion.UsuarioAceptanteId = usuario.UsuarioId;

        // Todos los repositorios comparten el mismo contexto de datos: un solo guardado persiste todo junto.
        await _invitacionRepositorio.GuardarCambiosAsync();

        var empresa = invitacion.Empresa ?? await _empresaRepositorio.ObtenerPorIdAsync(invitacion.EmpresaId);
        await _notificacionServicio.CrearAsync(
            invitacion.UsuarioInvitadorId,
            "INVITACION_ACEPTADA",
            "Invitación aceptada",
            $"{usuario.NombreCompleto} aceptó unirse a {empresa?.Nombre}. Ya puedes asignarle un rol desde Empleados.");
    }

    private async Task<InvitacionEmpresa> ObtenerPorTokenAsync(string token)
    {
        var invitacionId = TokenInvitacionTexto.ExtraerInvitacionId(token ?? string.Empty)
            ?? throw new InvalidOperationException(MensajeTokenInvalido);
        var invitacion = await _invitacionRepositorio.ObtenerPorIdAsync(invitacionId);
        if (invitacion is null || string.IsNullOrEmpty(invitacion.TokenHash) || !_hasheadorContrasenas.Verificar(token!, invitacion.TokenHash))
        {
            throw new InvalidOperationException(MensajeTokenInvalido);
        }

        return invitacion;
    }

    private static void AsegurarQuePuedeAceptarse(InvitacionEmpresa invitacion)
    {
        if (invitacion.FechaAceptacion is not null)
        {
            throw new InvalidOperationException("Esta invitación ya fue aceptada.");
        }

        if (!ReglasInvitacionEmpresa.PuedeAceptarse(invitacion, DateTime.UtcNow))
        {
            throw new InvalidOperationException("Esta invitación venció. Pide a quien te invitó que te envíe una nueva.");
        }
    }
}
