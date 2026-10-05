using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IEliminacionEmpresaRepositorio"/> con Entity
/// Framework Core. Todas las claves foráneas son restrictivas, así que el
/// borrado sigue un orden fijo, de lo más dependiente a la empresa. No decide
/// reglas de negocio: solo consulta y borra.
/// </summary>
public class EliminacionEmpresaRepositorio : IEliminacionEmpresaRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio con el contexto de datos.</summary>
    public EliminacionEmpresaRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<bool> TieneRutaEnCursoAsync(int empresaId)
    {
        return await _contexto.Servicios.AnyAsync(s => s.Estado == EstadoServicio.EN_CURSO && s.Jornada!.EmpresaId == empresaId);
    }

    /// <inheritdoc />
    public async Task<List<string>> EliminarAsync(int empresaId)
    {
        await using var transaccion = await _contexto.Database.BeginTransactionAsync();

        var empleadoIds = await _contexto.Empleados.Where(e => e.EmpresaId == empresaId).Select(e => e.EmpleadoId).ToListAsync();
        var servicioIds = await _contexto.Servicios.Where(s => s.Jornada!.EmpresaId == empresaId).Select(s => s.ServicioId).ToListAsync();
        var archivos = await EliminarOperacionAsync(empresaId, servicioIds, empleadoIds);
        await EliminarPersonasAsync(empresaId, empleadoIds);
        await EliminarConfiguracionAsync(empresaId);

        await _contexto.Empresas.Where(e => e.EmpresaId == empresaId).ExecuteDeleteAsync();
        await transaccion.CommitAsync();
        return archivos;
    }

    /// <summary>
    /// Borra jornadas, rutas, pasajeros y programaciones (con sus chats,
    /// incidencias y evidencias). Incluye los pasajes de los empleados de la
    /// empresa en rutas de otra (si antes pertenecieron a ella): sin eso no
    /// se podría borrar su ficha de empleado.
    /// </summary>
    private async Task<List<string>> EliminarOperacionAsync(int empresaId, List<int> servicioIds, List<int> empleadoIds)
    {
        var pasajeroIds = await _contexto.ServiciosPasajero
            .Where(p => servicioIds.Contains(p.ServicioId) || empleadoIds.Contains(p.EmpleadoId))
            .Select(p => p.ServicioPasajeroId).ToListAsync();
        var incidenciaIds = await _contexto.Incidencias.Where(i => pasajeroIds.Contains(i.ServicioPasajeroId)).Select(i => i.IncidenciaId).ToListAsync();
        var archivos = await _contexto.Evidencias.Where(e => incidenciaIds.Contains(e.IncidenciaId)).Select(e => e.ReferenciaArchivo).ToListAsync();
        var conversacionIds = await _contexto.Conversaciones.Where(c => pasajeroIds.Contains(c.ServicioPasajeroId)).Select(c => c.ConversacionId).ToListAsync();

        await _contexto.Evidencias.Where(e => incidenciaIds.Contains(e.IncidenciaId)).ExecuteDeleteAsync();
        await _contexto.Incidencias.Where(i => incidenciaIds.Contains(i.IncidenciaId)).ExecuteDeleteAsync();
        await _contexto.Mensajes.Where(m => conversacionIds.Contains(m.ConversacionId)).ExecuteDeleteAsync();
        await _contexto.Conversaciones.Where(c => conversacionIds.Contains(c.ConversacionId)).ExecuteDeleteAsync();
        await _contexto.ServiciosPasajero.Where(p => pasajeroIds.Contains(p.ServicioPasajeroId)).ExecuteDeleteAsync();
        await _contexto.Servicios.Where(s => servicioIds.Contains(s.ServicioId)).ExecuteDeleteAsync();
        await _contexto.ProgramacionesTransporte.Where(p => p.EmpresaId == empresaId || empleadoIds.Contains(p.EmpleadoId)).ExecuteDeleteAsync();
        await _contexto.Jornadas.Where(j => j.EmpresaId == empresaId).ExecuteDeleteAsync();

        return archivos;
    }

    /// <summary>
    /// Desvincula a las personas de la empresa. Las cuentas se conservan sin
    /// empresa; solo se borran las que nunca se activaron (sin contraseña) y
    /// no tienen ninguna otra relación: suelen ser pasajeros creados por una
    /// importación.
    /// </summary>
    private async Task EliminarPersonasAsync(int empresaId, List<int> empleadoIds)
    {
        var sinActivar = await _contexto.Empleados
            .Where(e => e.EmpresaId == empresaId && e.Usuario!.PasswordHash == null)
            .Where(e => !_contexto.Conductores.Any(c => c.UsuarioId == e.UsuarioId))
            .Where(e => !_contexto.UsuarioRoles.Any(r => r.UsuarioId == e.UsuarioId && r.EmpresaId != empresaId && r.Rol != Rol.EMPLEADO))
            .Select(e => e.UsuarioId).ToListAsync();

        await _contexto.UbicacionesRecogidaHistorica.Where(u => empleadoIds.Contains(u.EmpleadoId)).ExecuteDeleteAsync();
        await _contexto.Empleados.Where(e => empleadoIds.Contains(e.EmpleadoId)).ExecuteDeleteAsync();
        await _contexto.VinculacionesConductorEmpresa.Where(v => v.EmpresaId == empresaId).ExecuteDeleteAsync();
        await _contexto.ImportacionesExcel.Where(i => i.EmpresaId == empresaId).ExecuteDeleteAsync();
        await _contexto.InvitacionesEmpresa.Where(i => i.EmpresaId == empresaId).ExecuteDeleteAsync();

        // El rol EMPLEADO vuelve a quedar sin empresa (como en una cuenta recién registrada); el de coordinador de esta empresa desaparece.
        await _contexto.UsuarioRoles.Where(r => r.EmpresaId == empresaId && r.Rol != Rol.EMPLEADO).ExecuteDeleteAsync();
        await _contexto.UsuarioRoles.Where(r => r.EmpresaId == empresaId)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(r => r.EmpresaId, (int?)null));

        await _contexto.Notificaciones.Where(n => sinActivar.Contains(n.UsuarioId)).ExecuteDeleteAsync();
        await _contexto.TokensVerificacion.Where(t => sinActivar.Contains(t.UsuarioId)).ExecuteDeleteAsync();
        await _contexto.InvitacionesEmpresa.Where(i => i.UsuarioAceptanteId != null && sinActivar.Contains(i.UsuarioAceptanteId.Value))
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(i => i.UsuarioAceptanteId, (int?)null));
        await _contexto.UsuarioRoles.Where(r => sinActivar.Contains(r.UsuarioId)).ExecuteDeleteAsync();
        await _contexto.Usuarios.Where(u => sinActivar.Contains(u.UsuarioId)).ExecuteDeleteAsync();
    }

    /// <summary>Borra la configuración de la empresa y sus registros de facturación.</summary>
    private async Task EliminarConfiguracionAsync(int empresaId)
    {
        await _contexto.Zonas.Where(z => z.EmpresaId == empresaId).ExecuteDeleteAsync();
        await _contexto.MacroZonas.Where(m => m.EmpresaId == empresaId).ExecuteDeleteAsync();
        await _contexto.CorredoresViales.Where(c => c.EmpresaId == empresaId).ExecuteDeleteAsync();
        await _contexto.BarrerasGeograficas.Where(b => b.EmpresaId == empresaId).ExecuteDeleteAsync();
        await _contexto.PlantillasColumnasPegado.Where(p => p.EmpresaId == empresaId).ExecuteDeleteAsync();
        await _contexto.Sedes.Where(s => s.EmpresaId == empresaId).ExecuteDeleteAsync();
        await _contexto.UsosConductor.Where(u => u.EmpresaId == empresaId).ExecuteDeleteAsync();
        await _contexto.CierresMensuales.Where(c => c.EmpresaId == empresaId).ExecuteDeleteAsync();
    }
}
