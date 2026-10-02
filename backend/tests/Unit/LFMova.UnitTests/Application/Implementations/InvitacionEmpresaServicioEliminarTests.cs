using LFMova.Application.DTOs.Invitaciones;
using LFMova.Application.Implementations;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Pruebas del borrado de invitaciones de la lista de una empresa. Usan los
/// mismos repositorios falsos que el resto de pruebas de <see cref="InvitacionEmpresaServicio"/>.
/// </summary>
public partial class InvitacionEmpresaServicioTests
{
    [Fact]
    public async Task EliminarAsync_BorraLaInvitacionDeLaEmpresa()
    {
        var c = Crear();
        await c.Servicio.InvitarAsync(EmpresaId, new CrearInvitacionEmpresaDto { Cedula = "1001", Correo = "nuevo@test.com" }, CoordinadorId);
        var invitacion = Assert.Single(c.Invitaciones.Invitaciones);

        await c.Servicio.EliminarAsync(EmpresaId, invitacion.InvitacionEmpresaId);

        Assert.Empty(c.Invitaciones.Invitaciones);
    }

    [Fact]
    public async Task EliminarAsync_LanzaExcepcion_CuandoLaInvitacionEsDeOtraEmpresa()
    {
        var c = Crear();
        await c.Servicio.InvitarAsync(EmpresaId, new CrearInvitacionEmpresaDto { Cedula = "1001", Correo = "nuevo@test.com" }, CoordinadorId);
        var invitacion = Assert.Single(c.Invitaciones.Invitaciones);

        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Servicio.EliminarAsync(EmpresaId + 1, invitacion.InvitacionEmpresaId));

        Assert.Single(c.Invitaciones.Invitaciones);
    }
}
