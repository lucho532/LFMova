using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LFMova.Domain.Entities;

namespace LFMova.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="UsuarioRol"/>: relación con
/// <see cref="Usuario"/> y con <see cref="Empresa"/> (opcional). No valida
/// aquí las reglas de negocio de asignación de roles (ver
/// <c>LFMova.Domain.Rules.ReglasUsuarioRol</c>).
/// </summary>
public class UsuarioRolConfiguration : IEntityTypeConfiguration<UsuarioRol>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="UsuarioRol"/>.</summary>
    public void Configure(EntityTypeBuilder<UsuarioRol> builder)
    {
        builder.HasKey(ur => ur.UsuarioRolId);

        builder.HasOne(ur => ur.Usuario)
            .WithMany(u => u.UsuarioRoles)
            .HasForeignKey(ur => ur.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ur => ur.Empresa)
            .WithMany()
            .HasForeignKey(ur => ur.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ur => ur.UsuarioId);
        builder.HasIndex(ur => ur.EmpresaId);
    }
}
