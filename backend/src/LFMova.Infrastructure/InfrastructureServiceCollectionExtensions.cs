using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LFMova.Application.Interfaces;
using LFMova.Infrastructure.Almacenamiento;
using LFMova.Infrastructure.Autenticacion;
using LFMova.Infrastructure.Correo;
using LFMova.Infrastructure.Data;
using LFMova.Infrastructure.Excel;
using LFMova.Infrastructure.Repositories;

namespace LFMova.Infrastructure;

/// <summary>
/// Contiene los métodos de extensión para registrar los servicios de la capa de
/// infraestructura (persistencia, repositorios, generación de JWT) en el
/// contenedor de inyección de dependencias de la aplicación.
/// Su responsabilidad es exclusivamente el cableado técnico de dependencias.
/// No debe contener reglas de negocio.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registra <see cref="LFMovaDbContext"/> configurado para PostgreSQL,
    /// los repositorios y el generador de JWT, utilizando la configuración
    /// externa de la aplicación (appsettings, variables de entorno o secretos
    /// de desarrollo).
    /// </summary>
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        var cadenaConexion = configuracion.GetConnectionString("LFMovaDb");

        servicios.AddDbContext<LFMovaDbContext>(opciones =>
            opciones.UseNpgsql(cadenaConexion));

        servicios.Configure<OpcionesJwt>(configuracion.GetSection(OpcionesJwt.Seccion));
        servicios.AddSingleton<IGeneradorTokenJwt, GeneradorTokenJwt>();

        servicios.Configure<OpcionesBrevo>(configuracion.GetSection(OpcionesBrevo.Seccion));
        servicios.AddHttpClient<IServicioCorreo, ServicioCorreoBrevo>();

        servicios.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        servicios.AddScoped<IEmpresaRepositorio, EmpresaRepositorio>();
        servicios.AddScoped<IUsuarioRolRepositorio, UsuarioRolRepositorio>();
        servicios.AddScoped<ISedeRepositorio, SedeRepositorio>();
        servicios.AddScoped<IZonaRepositorio, ZonaRepositorio>();
        servicios.AddScoped<IPlantillaColumnasPegadoRepositorio, PlantillaColumnasPegadoRepositorio>();
        servicios.AddScoped<IMacroZonaRepositorio, MacroZonaRepositorio>();
        servicios.AddScoped<IBarreraGeograficaRepositorio, BarreraGeograficaRepositorio>();
        servicios.AddScoped<ICorredorVialRepositorio, CorredorVialRepositorio>();
        servicios.AddScoped<IConductorRepositorio, ConductorRepositorio>();
        servicios.AddScoped<IVehiculoRepositorio, VehiculoRepositorio>();
        servicios.AddScoped<IUnidadOperativaRepositorio, UnidadOperativaRepositorio>();
        servicios.AddScoped<IEmpleadoRepositorio, EmpleadoRepositorio>();
        servicios.AddScoped<IEliminacionPersonaRepositorio, EliminacionPersonaRepositorio>();
        servicios.AddScoped<IEliminacionEmpresaRepositorio, EliminacionEmpresaRepositorio>();
        servicios.AddScoped<IImportacionExcelRepositorio, ImportacionExcelRepositorio>();
        servicios.AddScoped<IEstadisticasRepositorio, EstadisticasRepositorio>();
        servicios.AddScoped<IFacturacionRepositorio, FacturacionRepositorio>();
        servicios.AddSingleton<IAlmacenamientoArchivos, AlmacenamientoLocalArchivos>();
        servicios.AddSingleton<IExcelProgramacionLector, ExcelProgramacionLectorClosedXml>();
        servicios.AddSingleton<IGeneradorSoporteRutas, GeneradorSoporteRutasClosedXml>();
        servicios.AddSingleton<IGeneradorCierreMensual, GeneradorCierreMensualClosedXml>();
        servicios.AddScoped<IProgramacionTransporteRepositorio, ProgramacionTransporteRepositorio>();
        servicios.AddScoped<IJornadaRepositorio, JornadaRepositorio>();
        servicios.AddScoped<IServicioRepositorio, ServicioRepositorio>();
        servicios.AddScoped<IServicioPasajeroRepositorio, ServicioPasajeroRepositorio>();
        servicios.AddScoped<IUbicacionRecogidaHistoricaRepositorio, UbicacionRecogidaHistoricaRepositorio>();
        servicios.AddScoped<INotificacionRepositorio, NotificacionRepositorio>();
        servicios.AddScoped<ITokenVerificacionRepositorio, TokenVerificacionRepositorio>();
        servicios.AddScoped<IInvitacionEmpresaRepositorio, InvitacionEmpresaRepositorio>();
        servicios.AddScoped<IConversacionRepositorio, ConversacionRepositorio>();
        servicios.AddScoped<IMensajeRepositorio, MensajeRepositorio>();
        servicios.AddScoped<IIncidenciaRepositorio, IncidenciaRepositorio>();
        servicios.AddScoped<IEvidenciaRepositorio, EvidenciaRepositorio>();

        return servicios;
    }
}
