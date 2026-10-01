using Microsoft.EntityFrameworkCore;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Infrastructure.Data;

namespace LFMova.Infrastructure.Repositories;

/// <summary>
/// Implementa <see cref="IInvitacionEmpresaRepositorio"/> mediante Entity
/// Framework Core. Su responsabilidad es exclusivamente la persistencia; no
/// decide reglas de negocio.
/// </summary>
public class InvitacionEmpresaRepositorio : IInvitacionEmpresaRepositorio
{
    private readonly LFMovaDbContext _contexto;

    /// <summary>Crea el repositorio utilizando el contexto de base de datos.</summary>
    public InvitacionEmpresaRepositorio(LFMovaDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<InvitacionEmpresa?> ObtenerPorIdAsync(int invitacionEmpresaId)
    {
        return await _contexto.InvitacionesEmpresa
            .Include(i => i.Empresa)
            .Include(i => i.UsuarioInvitador)
            .FirstOrDefaultAsync(i => i.InvitacionEmpresaId == invitacionEmpresaId);
    }

    /// <inheritdoc />
    public async Task<List<InvitacionEmpresa>> ObtenerPorEmpresaAsync(int empresaId)
    {
        return await _contexto.InvitacionesEmpresa
            .Include(i => i.UsuarioAceptante)
            .Where(i => i.EmpresaId == empresaId)
            .OrderByDescending(i => i.FechaCreacion)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<InvitacionEmpresa>> ObtenerPorEmpresaYCedulaAsync(int empresaId, string cedula)
    {
        return await _contexto.InvitacionesEmpresa
            .Where(i => i.EmpresaId == empresaId && i.Cedula == cedula)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(InvitacionEmpresa invitacion)
    {
        await _contexto.InvitacionesEmpresa.AddAsync(invitacion);
    }

    /// <inheritdoc />
    public async Task GuardarCambiosAsync()
    {
        await _contexto.SaveChangesAsync();
    }
}
