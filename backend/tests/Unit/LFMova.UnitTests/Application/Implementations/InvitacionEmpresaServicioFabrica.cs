using LFMova.Application.DTOs.Autenticacion;
using LFMova.Application.Implementations;
using LFMova.Application.Implementations.Invitaciones;
using LFMova.Application.Interfaces;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Arma un <see cref="InvitacionEmpresaServicio"/> real con su colaborador a partir de los
/// repositorios y servicios que cada prueba decide (normalmente falsos en memoria), igual que lo haría
/// el contenedor de dependencias. No contiene aserciones ni datos de prueba.
/// </summary>
internal static class InvitacionEmpresaServicioFabrica
{
    /// <summary>Crea el servicio conectando su colaborador con las dependencias indicadas.</summary>
    public static InvitacionEmpresaServicio Crear(
        IInvitacionEmpresaRepositorio invitacionRepositorio,
        IUsuarioRepositorio usuarioRepositorio,
        IUsuarioRolRepositorio usuarioRolRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IEmpresaRepositorio empresaRepositorio,
        IPersonaServicio personaServicio,
        INotificacionServicio notificacionServicio,
        IHasheadorContrasenas hasheadorContrasenas,
        IServicioCorreo servicioCorreo,
        OpcionesFrontend opcionesFrontend)
        => new(
            invitacionRepositorio, usuarioRepositorio, usuarioRolRepositorio, empleadoRepositorio, empresaRepositorio,
            notificacionServicio, hasheadorContrasenas,
            new EmisorInvitacionEmpresa(
                invitacionRepositorio, usuarioRepositorio, empresaRepositorio, personaServicio, hasheadorContrasenas, servicioCorreo, opcionesFrontend));
}
