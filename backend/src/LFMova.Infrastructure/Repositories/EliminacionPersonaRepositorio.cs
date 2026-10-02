using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IEliminacionPersonaRepositorio"/> con Entity
/// Framework Core. Cada borrado corre en una transacción y respeta el orden
/// que exigen las claves foráneas (todas son restrictivas). No decide reglas
/// de negocio: solo consulta y borra.
/// </summary>
public class EliminacionPersonaRepositorio : IEliminacionPersonaRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio con el contexto de datos.</summary>
    public EliminacionPersonaRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<bool> TieneRutaEnCursoAsync(int usuarioId)
    {
        var conduce = await _contexto.Servicios.AnyAsync(s =>
            s.Estado == EstadoServicio.EN_CURSO && s.UnidadOperativa!.Conductor!.UsuarioId == usuarioId);
        return conduce || await _contexto.ServiciosPasajero.AnyAsync(p =>
            p.Servicio!.Estado == EstadoServicio.EN_CURSO && p.Empleado!.UsuarioId == usuarioId);
    }

    /// <inheritdoc />
    public async Task<bool> TieneRegistrosDeCoordinacionAsync(int usuarioId)
    {
        return await _contexto.ImportacionesExcel.AnyAsync(i => i.CoordinadorId == usuarioId)
            || await _contexto.InvitacionesEmpresa.AnyAsync(i => i.UsuarioInvitadorId == usuarioId);
    }

    /// <inheritdoc />
    public async Task<List<int>> ObtenerEmpresasRelacionadasAsync(int usuarioId)
    {
        var porRol = await _contexto.UsuarioRoles.Where(r => r.UsuarioId == usuarioId && r.EmpresaId != null).Select(r => r.EmpresaId!.Value).ToListAsync();
        var porEmpleado = await _contexto.Empleados.Where(e => e.UsuarioId == usuarioId).Select(e => e.EmpresaId).ToListAsync();
        var porConductor = await _contexto.VinculacionesConductorEmpresa.Where(v => v.Conductor!.UsuarioId == usuarioId).Select(v => v.EmpresaId).ToListAsync();
        var porInvitacion = await _contexto.InvitacionesEmpresa.Where(i => i.UsuarioAceptanteId == usuarioId).Select(i => i.EmpresaId).ToListAsync();
        return porRol.Concat(porEmpleado).Concat(porConductor).Concat(porInvitacion).Distinct().ToList();
    }

    /// <inheritdoc />
    public async Task<List<string>> EliminarCuentaAsync(int usuarioId)
    {
        await using var transaccion = await _contexto.Database.BeginTransactionAsync();

        var cedula = await _contexto.Usuarios.Where(u => u.UsuarioId == usuarioId).Select(u => u.Cedula).FirstAsync();
        var empleadoIds = await _contexto.Empleados.Where(e => e.UsuarioId == usuarioId).Select(e => e.EmpleadoId).ToListAsync();
        var archivos = await EliminarComoPasajeroAsync(empleadoIds);

        var conductorIds = await _contexto.Conductores.Where(c => c.UsuarioId == usuarioId).Select(c => c.ConductorId).ToListAsync();
        var unidadIds = await _contexto.UnidadesOperativas.Where(u => conductorIds.Contains(u.ConductorId)).Select(u => u.UnidadOperativaId).ToListAsync();
        await LiberarServiciosAsync(unidadIds, empresaId: null);

        // Mensajes que escribió como conductor en conversaciones de otros pasajeros.
        await _contexto.Mensajes.Where(m => m.UsuarioId == usuarioId).ExecuteDeleteAsync();
        await _contexto.UnidadesOperativas.Where(u => conductorIds.Contains(u.ConductorId)).ExecuteDeleteAsync();
        await _contexto.Vehiculos.Where(v => conductorIds.Contains(v.ConductorId)).ExecuteDeleteAsync();
        await _contexto.VinculacionesConductorEmpresa.Where(v => conductorIds.Contains(v.ConductorId)).ExecuteDeleteAsync();
        await _contexto.Conductores.Where(c => c.UsuarioId == usuarioId).ExecuteDeleteAsync();

        await _contexto.Notificaciones.Where(n => n.UsuarioId == usuarioId).ExecuteDeleteAsync();
        await _contexto.TokensVerificacion.Where(t => t.UsuarioId == usuarioId).ExecuteDeleteAsync();
        await _contexto.InvitacionesEmpresa.Where(i => i.UsuarioAceptanteId == usuarioId || i.Cedula == cedula).ExecuteDeleteAsync();
        await _contexto.UsuarioRoles.Where(r => r.UsuarioId == usuarioId).ExecuteDeleteAsync();
        await _contexto.Usuarios.Where(u => u.UsuarioId == usuarioId).ExecuteDeleteAsync();

        await transaccion.CommitAsync();
        return archivos;
    }

    /// <inheritdoc />
    public async Task<List<string>> QuitarDeEmpresaAsync(int usuarioId, int empresaId)
    {
        await using var transaccion = await _contexto.Database.BeginTransactionAsync();

        var cedula = await _contexto.Usuarios.Where(u => u.UsuarioId == usuarioId).Select(u => u.Cedula).FirstAsync();
        var empleadoIds = await _contexto.Empleados.Where(e => e.UsuarioId == usuarioId && e.EmpresaId == empresaId).Select(e => e.EmpleadoId).ToListAsync();
        var archivos = await EliminarComoPasajeroAsync(empleadoIds);

        var conductorIds = await _contexto.Conductores.Where(c => c.UsuarioId == usuarioId).Select(c => c.ConductorId).ToListAsync();
        var unidadIds = await _contexto.UnidadesOperativas.Where(u => conductorIds.Contains(u.ConductorId)).Select(u => u.UnidadOperativaId).ToListAsync();
        await LiberarServiciosAsync(unidadIds, empresaId);

        await _contexto.VinculacionesConductorEmpresa.Where(v => conductorIds.Contains(v.ConductorId) && v.EmpresaId == empresaId).ExecuteDeleteAsync();
        await _contexto.InvitacionesEmpresa.Where(i => i.EmpresaId == empresaId && (i.UsuarioAceptanteId == usuarioId || i.Cedula == cedula)).ExecuteDeleteAsync();

        // El rol EMPLEADO se conserva, pero vuelve a quedar sin empresa (como en una cuenta recién registrada).
        await _contexto.UsuarioRoles
            .Where(r => r.UsuarioId == usuarioId && r.Rol == Rol.EMPLEADO && r.EmpresaId == empresaId)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(r => r.EmpresaId, (int?)null));

        await transaccion.CommitAsync();
        return archivos;
    }

    /// <summary>
    /// Borra las fichas de empleado indicadas con todo su historial como
    /// pasajeras (incidencias, evidencias, chat, programaciones, ubicaciones)
    /// y devuelve las referencias de los archivos de evidencia borrados.
    /// </summary>
    private async Task<List<string>> EliminarComoPasajeroAsync(List<int> empleadoIds)
    {
        var pasajeroIds = await _contexto.ServiciosPasajero.Where(p => empleadoIds.Contains(p.EmpleadoId)).Select(p => p.ServicioPasajeroId).ToListAsync();
        var incidenciaIds = await _contexto.Incidencias.Where(i => pasajeroIds.Contains(i.ServicioPasajeroId)).Select(i => i.IncidenciaId).ToListAsync();
        var archivos = await _contexto.Evidencias.Where(e => incidenciaIds.Contains(e.IncidenciaId)).Select(e => e.ReferenciaArchivo).ToListAsync();
        var conversacionIds = await _contexto.Conversaciones.Where(c => pasajeroIds.Contains(c.ServicioPasajeroId)).Select(c => c.ConversacionId).ToListAsync();

        await _contexto.Evidencias.Where(e => incidenciaIds.Contains(e.IncidenciaId)).ExecuteDeleteAsync();
        await _contexto.Incidencias.Where(i => incidenciaIds.Contains(i.IncidenciaId)).ExecuteDeleteAsync();
        await _contexto.Mensajes.Where(m => conversacionIds.Contains(m.ConversacionId)).ExecuteDeleteAsync();
        await _contexto.Conversaciones.Where(c => conversacionIds.Contains(c.ConversacionId)).ExecuteDeleteAsync();
        await _contexto.ServiciosPasajero.Where(p => pasajeroIds.Contains(p.ServicioPasajeroId)).ExecuteDeleteAsync();
        await _contexto.ProgramacionesTransporte.Where(p => empleadoIds.Contains(p.EmpleadoId)).ExecuteDeleteAsync();
        await _contexto.UbicacionesRecogidaHistorica.Where(u => empleadoIds.Contains(u.EmpleadoId)).ExecuteDeleteAsync();
        await _contexto.Empleados.Where(e => empleadoIds.Contains(e.EmpleadoId)).ExecuteDeleteAsync();

        return archivos;
    }

    /// <summary>
    /// Deja sin unidad los servicios que usaban las unidades indicadas (todos,
    /// o solo los de una empresa) y ajusta su estado según
    /// <see cref="ReglasEstadoServicio.EstadoAlQuedarSinUnidad"/>.
    /// </summary>
    private async Task LiberarServiciosAsync(List<int> unidadIds, int? empresaId)
    {
        var servicios = await _contexto.Servicios
            .Where(s => s.UnidadOperativaId != null && unidadIds.Contains(s.UnidadOperativaId.Value))
            .Where(s => empresaId == null || s.Jornada!.EmpresaId == empresaId)
            .ToListAsync();

        foreach (var servicio in servicios)
        {
            servicio.UnidadOperativaId = null;
            servicio.Estado = ReglasEstadoServicio.EstadoAlQuedarSinUnidad(servicio.Estado);
        }

        await _contexto.SaveChangesAsync();
    }
}
