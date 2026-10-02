using Microsoft.Extensions.DependencyInjection;
using LFMova.Application.Implementations;
using LFMova.Application.Implementations.Conductores;
using LFMova.Application.Implementations.Importaciones;
using LFMova.Application.Implementations.Invitaciones;
using LFMova.Application.Implementations.Jornadas;
using LFMova.Application.Implementations.Servicios;
using LFMova.Application.Implementations.ServiciosPasajero;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;

namespace LFMova.Application;

/// <summary>
/// Contiene los métodos de extensión para registrar los servicios de casos
/// de uso de la capa de aplicación en el contenedor de inyección de
/// dependencias. Su responsabilidad es exclusivamente el cableado técnico de
/// dependencias; no contiene reglas de negocio.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registra los servicios de la capa de aplicación.</summary>
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
        servicios.AddSingleton<IHasheadorContrasenas, HasheadorContrasenas>();

        servicios.AddScoped<IEmpresaServicio, EmpresaServicio>();
        servicios.AddScoped<IAsignacionCoordinadorServicio, AsignacionCoordinadorServicio>();
        servicios.AddScoped<ISedeServicio, SedeServicio>();
        servicios.AddScoped<IZonaServicio, ZonaServicio>();
        servicios.AddScoped<IZonaReorganizacionServicio, ZonaReorganizacionServicio>();
        servicios.AddScoped<IPlantillaColumnasPegadoServicio, PlantillaColumnasPegadoServicio>();
        servicios.AddScoped<IMacroZonaServicio, MacroZonaServicio>();
        servicios.AddScoped<IBarreraGeograficaServicio, BarreraGeograficaServicio>();
        servicios.AddScoped<ICorredorVialServicio, CorredorVialServicio>();
        servicios.AddScoped<IConductorServicio, ConductorServicio>();
        servicios.AddScoped<IVehiculoServicio, VehiculoServicio>();
        servicios.AddScoped<IUnidadOperativaServicio, UnidadOperativaServicio>();
        servicios.AddScoped<IEmpleadoServicio, EmpleadoServicio>();
        servicios.AddScoped<IProgramacionTransporteServicio, ProgramacionTransporteServicio>();
        servicios.AddScoped<IJornadaServicio, JornadaServicio>();
        servicios.AddScoped<IServicioServicio, ServicioServicio>();
        servicios.AddScoped<IServicioPasajeroServicio, ServicioPasajeroServicio>();
        servicios.AddScoped<IPersonaServicio, PersonaServicio>();
        servicios.AddScoped<IImportacionExcelServicio, ImportacionExcelServicio>();
        servicios.AddScoped<ICuentaServicio, CuentaServicio>();
        servicios.AddScoped<IRegistroServicio, RegistroServicio>();
        servicios.AddScoped<IInvitacionEmpresaServicio, InvitacionEmpresaServicio>();
        servicios.AddScoped<IRecuperacionContrasenaServicio, RecuperacionContrasenaServicio>();
        servicios.AddScoped<INotificacionServicio, NotificacionServicio>();
        servicios.AddScoped<IChatServicio, ChatServicio>();
        servicios.AddScoped<IIncidenciaServicio, IncidenciaServicio>();
        servicios.AddScoped<IAlertaEjecucionServicio, AlertaEjecucionServicio>();

        AgregarColaboradores(servicios);

        return servicios;
    }

    /// <summary>
    /// Registra los colaboradores en los que se dividen los servicios más grandes. No tienen interfaz
    /// porque no son contratos de la capa: cada uno lo usa únicamente el servicio al que pertenece.
    /// </summary>
    private static void AgregarColaboradores(IServiceCollection servicios)
    {
        // ConductorServicio, JornadaServicio e InvitacionEmpresaServicio.
        servicios.AddScoped<RegistroConductor>();
        servicios.AddScoped<DepuradorJornada>();
        servicios.AddScoped<EnviadorSoporteRutas>();
        servicios.AddScoped<ArmadorSoporteRutas>();
        servicios.AddScoped<ExportadorSoporteJornada>();
        servicios.AddScoped<EmisorInvitacionEmpresa>();

        // ServicioServicio.
        servicios.AddScoped<AccesoServicio>();
        servicios.AddScoped<AsignadorUnidadServicio>();
        servicios.AddScoped<ModificadorRutaArmada>();
        servicios.AddScoped<EjecucionServicio>();

        // ServicioPasajeroServicio.
        servicios.AddScoped<AccesoServicioPasajero>();
        servicios.AddScoped<ConsultasServicioPasajero>();
        servicios.AddScoped<NotificadorServicioPasajero>();
        servicios.AddScoped<GestorParticipacionPasajero>();
        servicios.AddScoped<GestorUbicacionPasajero>();
        servicios.AddScoped<ReorganizadorPasajeros>();
        servicios.AddScoped<AprendizCorredorVial>();

        // ImportacionExcelServicio.
        servicios.AddScoped<AnalizadorImportacion>();
        servicios.AddScoped<EjecutorImportacion>();
        servicios.AddScoped<CargadorJornadasImportacion>();
        servicios.AddScoped<AgrupadorPendientesRuta>();
        servicios.AddScoped<PlanificadorDestinosRuta>();
        servicios.AddScoped<AseguradorEmpleadoImportacion>();
        servicios.AddScoped<ResolutorSedeImportacion>();
        servicios.AddScoped<CreadorRutaVacia>();
        servicios.AddScoped<CreadorRutaPegada>();
    }
}
