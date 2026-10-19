using System.Net.Http.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LFMova.Application.DTOs.Autenticacion;
using LFMova.Application.DTOs.Conductores;
using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.DTOs.Vehiculos;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Data;
using LFMova.IntegrationTests.Fixtures;

namespace LFMova.IntegrationTests;

/// <summary>
/// Pruebas de una segunda importación que cae sobre una ruta que ya se publicó
/// o que el conductor ya inició (decisión del 2026-10-02).
/// </summary>
public partial class ImportacionExcelTests
{
    [Fact]
    public async Task ImportarSobreUnaRutaPublicada_LaCompletaYLaDejaSinPublicarParaRevisarla()
    {
        var (cliente, ruta, contexto) = await PrepararEmpresaConUnConductorAsync("PUB");
        using var _ = cliente;
        var primero = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, HojaDeUnPasajero(8101L, "Primero"), "2026-10-07", null, repartir: true);
        var servicioId = (await contexto.Servicios.AsNoTracking().SingleAsync(s => s.JornadaId == primero.JornadaId)).ServicioId;
        await contexto.Servicios.Where(s => s.ServicioId == servicioId).ExecuteUpdateAsync(c => c.SetProperty(s => s.Estado, EstadoServicio.PUBLICADO));

        var segundo = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, HojaDeUnPasajero(8102L, "Segundo"), "2026-10-07", null, repartir: true);

        Assert.Equal(0, segundo.ServiciosCreados);
        Assert.Contains(segundo.Advertencias, a => a.Contains("ya estaba publicada"));
        var servicio = await contexto.Servicios.AsNoTracking().SingleAsync(s => s.ServicioId == servicioId);
        Assert.Equal(EstadoServicio.ASIGNADO, servicio.Estado);
        Assert.Equal(2, await contexto.ServiciosPasajero.CountAsync(sp => sp.ServicioId == servicioId));
    }

    [Fact]
    public async Task ImportarSobreUnaRutaEnCurso_NoLeAgregaPasajeros()
    {
        var (cliente, ruta, contexto) = await PrepararEmpresaConUnConductorAsync("CUR");
        using var _ = cliente;
        var primero = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, HojaDeUnPasajero(8201L, "Primero"), "2026-10-07", null, repartir: true);
        var servicioId = (await contexto.Servicios.AsNoTracking().SingleAsync(s => s.JornadaId == primero.JornadaId)).ServicioId;
        await contexto.Servicios.Where(s => s.ServicioId == servicioId).ExecuteUpdateAsync(c => c.SetProperty(s => s.Estado, EstadoServicio.EN_CURSO));

        await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, HojaDeUnPasajero(8202L, "Segundo"), "2026-10-07", null, repartir: true);

        var servicio = await contexto.Servicios.AsNoTracking().SingleAsync(s => s.ServicioId == servicioId);
        Assert.Equal(EstadoServicio.EN_CURSO, servicio.Estado);
        Assert.Equal(1, await contexto.ServiciosPasajero.CountAsync(sp => sp.ServicioId == servicioId));
    }

    /// <summary>
    /// Crea una empresa con su sede "Centro", su coordinador autenticado y un único conductor con
    /// capacidad para 3. Devuelve el cliente del coordinador, la ruta de importación y un contexto de datos.
    /// </summary>
    private async Task<(HttpClient Cliente, string Ruta, LFMovaDbContext Contexto)> PrepararEmpresaConUnConductorAsync(string sufijo)
    {
        var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, $"Empresa Ruta {sufijo}");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, $"RP-COORD-{sufijo}", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Manizales", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, $"RP-COORD-{sufijo}", "ClaveCoord123");

        var correo = $"rp.cond.{sufijo.ToLowerInvariant()}@pruebas.test";
        using (var anonimo = _fixture.Fabrica.CreateClient())
        {
            (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
            {
                Correo = correo, NombreCompleto = "Conductor Ruta", Cedula = $"RP-COND-{sufijo}", Telefono = "3000000099", Password = "ClaveCond123"
            })).EnsureSuccessStatusCode();
            (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken(correo) })).EnsureSuccessStatusCode();
        }

        await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, $"RP-COND-{sufijo}");
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
        {
            Cedula = $"RP-COND-{sufijo}",
            Vehiculo = new CrearVehiculoDto
            {
                Placa = $"RP{sufijo}", Marca = "Renault", Modelo = "2023", Capacidad = 3,
                VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
            }
        })).EnsureSuccessStatusCode();

        return (cliente, $"/api/empresas/{empresa.EmpresaId}/importaciones", contexto);
    }

    /// <summary>Hoja plana con un solo pasajero que entra a las 6:00 a la sede "Centro".</summary>
    private static byte[] HojaDeUnPasajero(long cedula, string nombre)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Datos");
        var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
        for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
        hoja.Cell(2, 1).Value = cedula;
        hoja.Cell(2, 2).Value = nombre;
        hoja.Cell(2, 3).Value = "Calle 1";
        hoja.Cell(2, 4).Value = "Centro";
        hoja.Cell(2, 5).Value = 3000000000L + cedula;
        hoja.Cell(2, 6).Value = "6:00";
        hoja.Cell(2, 7).Value = "Centro";

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }
}
