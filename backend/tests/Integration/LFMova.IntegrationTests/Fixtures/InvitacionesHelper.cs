using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LFMova.Application.DTOs.Invitaciones;
using LFMova.Application.Interfaces;
using LFMova.Infrastructure.Data;

namespace LFMova.IntegrationTests.Fixtures;

/// <summary>
/// Une a una persona ya registrada a la empresa de la única forma permitida
/// para un coordinador: el coordinador la invita por correo (por la API) y la
/// persona acepta el enlace. Lo usan las pruebas que necesitan que un
/// coordinador le asigne un rol (conductor, coordinador) a alguien que se
/// registró por su cuenta, sin que esa prueba trate sobre invitaciones.
/// </summary>
public static class InvitacionesHelper
{
    /// <summary>El coordinador del cliente indicado invita la cédula y la persona dueña de esa cédula acepta.</summary>
    public static async Task UnirAEmpresaAsync(PostgreSqlWebApplicationFactory fabrica, HttpClient clienteCoordinador, int empresaId, string cedula)
    {
        int usuarioId;
        string correo;
        using (var alcance = fabrica.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
            var usuario = await contexto.Usuarios.AsNoTracking().SingleAsync(u => u.Cedula == cedula);
            usuarioId = usuario.UsuarioId;
            // Una cuenta sembrada directamente en la base puede no tener correo: la invitación va entonces al escrito.
            correo = usuario.Email ?? $"{cedula.ToLowerInvariant()}@invitacion.test";
        }

        (await clienteCoordinador.PostAsJsonAsync($"/api/empresas/{empresaId}/invitaciones", new CrearInvitacionEmpresaDto
        {
            Cedula = cedula,
            Correo = correo
        })).EnsureSuccessStatusCode();

        var token = fabrica.Correo.ObtenerUltimoToken(correo);
        using var alcanceAceptar = fabrica.Services.CreateScope();
        await alcanceAceptar.ServiceProvider.GetRequiredService<IInvitacionEmpresaServicio>().AceptarAsync(token, usuarioId);
    }
}
