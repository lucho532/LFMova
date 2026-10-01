using Microsoft.EntityFrameworkCore;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Data;

/// <summary>
/// Representa el contexto de acceso a datos de la plataforma mediante Entity
/// Framework Core. Su responsabilidad es centralizar la configuración de
/// persistencia hacia PostgreSQL y exponer los conjuntos de entidades del
/// modelo de dominio.
/// No debe contener reglas de negocio ni lógica de autorización.
/// </summary>
public class TransportAppDbContext : DbContext
{
    /// <summary>
    /// Crea una nueva instancia del contexto utilizando las opciones configuradas
    /// externamente (proveedor PostgreSQL, cadena de conexión, etc.).
    /// </summary>
    public TransportAppDbContext(DbContextOptions<TransportAppDbContext> opciones)
        : base(opciones)
    {
    }

    /// <summary>Usuarios de la plataforma.</summary>
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    /// <summary>Roles asignados a los usuarios.</summary>
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();

    /// <summary>Empresas cliente de la plataforma.</summary>
    public DbSet<Empresa> Empresas => Set<Empresa>();

    /// <summary>Sedes de las empresas.</summary>
    public DbSet<Sede> Sedes => Set<Sede>();

    /// <summary>Zonas geográficas de recogida de las empresas.</summary>
    public DbSet<Zona> Zonas => Set<Zona>();

    /// <summary>Plantillas de orden de columnas para crear rutas pegando filas de un Excel externo.</summary>
    public DbSet<PlantillaColumnasPegado> PlantillasColumnasPegado => Set<PlantillaColumnasPegado>();

    /// <summary>Macrozonas (comunas) que organizan las zonas de las empresas.</summary>
    public DbSet<MacroZona> MacroZonas => Set<MacroZona>();

    /// <summary>Barreras geográficas declaradas entre barrios de las empresas.</summary>
    public DbSet<BarreraGeografica> BarrerasGeograficas => Set<BarreraGeografica>();

    /// <summary>Corredores viales (agrupaciones de zonas que comparten vehículo) de las empresas.</summary>
    public DbSet<CorredorVial> CorredoresViales => Set<CorredorVial>();

    /// <summary>Perfiles de empleado.</summary>
    public DbSet<Empleado> Empleados => Set<Empleado>();

    /// <summary>Perfiles de conductor.</summary>
    public DbSet<Conductor> Conductores => Set<Conductor>();

    /// <summary>Vinculaciones entre conductores y empresas.</summary>
    public DbSet<VinculacionConductorEmpresa> VinculacionesConductorEmpresa => Set<VinculacionConductorEmpresa>();

    /// <summary>Vehículos de los conductores.</summary>
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();

    /// <summary>Unidades operativas (conductor + vehículo).</summary>
    public DbSet<UnidadOperativa> UnidadesOperativas => Set<UnidadOperativa>();

    /// <summary>Importaciones de archivos Excel.</summary>
    public DbSet<ImportacionExcel> ImportacionesExcel => Set<ImportacionExcel>();

    /// <summary>Programaciones de transporte.</summary>
    public DbSet<ProgramacionTransporte> ProgramacionesTransporte => Set<ProgramacionTransporte>();

    /// <summary>Jornadas operativas.</summary>
    public DbSet<Jornada> Jornadas => Set<Jornada>();

    /// <summary>Servicios de transporte.</summary>
    public DbSet<Servicio> Servicios => Set<Servicio>();

    /// <summary>Participación de empleados en servicios.</summary>
    public DbSet<ServicioPasajero> ServiciosPasajero => Set<ServicioPasajero>();

    /// <summary>Ubicaciones de recogida históricas de los empleados.</summary>
    public DbSet<UbicacionRecogidaHistorica> UbicacionesRecogidaHistorica => Set<UbicacionRecogidaHistorica>();

    /// <summary>Incidencias registradas durante la ejecución de servicios.</summary>
    public DbSet<Incidencia> Incidencias => Set<Incidencia>();

    /// <summary>Evidencias asociadas a incidencias.</summary>
    public DbSet<Evidencia> Evidencias => Set<Evidencia>();

    /// <summary>Conversaciones entre conductor y empleado.</summary>
    public DbSet<Conversacion> Conversaciones => Set<Conversacion>();

    /// <summary>Mensajes de las conversaciones.</summary>
    public DbSet<Mensaje> Mensajes => Set<Mensaje>();

    /// <summary>Notificaciones dirigidas a los usuarios.</summary>
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    /// <summary>Tokens de verificación de correo y de restablecimiento de contraseña.</summary>
    public DbSet<TokenVerificacion> TokensVerificacion => Set<TokenVerificacion>();

    /// <summary>Invitaciones por correo para que una persona se una a una empresa.</summary>
    public DbSet<InvitacionEmpresa> InvitacionesEmpresa => Set<InvitacionEmpresa>();

    /// <summary>
    /// Aplica todas las configuraciones de entidades definidas mediante
    /// <see cref="Microsoft.EntityFrameworkCore.IEntityTypeConfiguration{TEntity}"/>
    /// en este mismo ensamblado (carpeta <c>Configurations</c>).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransportAppDbContext).Assembly);
    }
}
