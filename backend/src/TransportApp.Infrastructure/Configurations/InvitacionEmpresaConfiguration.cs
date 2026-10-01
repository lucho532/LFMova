using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="InvitacionEmpresa"/>. No
/// configura reglas de negocio.
/// </summary>
public class InvitacionEmpresaConfiguration : IEntityTypeConfiguration<InvitacionEmpresa>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="InvitacionEmpresa"/>.</summary>
    public void Configure(EntityTypeBuilder<InvitacionEmpresa> builder)
    {
        builder.HasKey(i => i.InvitacionEmpresaId);

        builder.Property(i => i.Cedula).IsRequired();
        builder.Property(i => i.Correo).IsRequired();
        builder.Property(i => i.TokenHash).IsRequired();

        builder.HasOne(i => i.Empresa)
            .WithMany()
            .HasForeignKey(i => i.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.UsuarioInvitador)
            .WithMany()
            .HasForeignKey(i => i.UsuarioInvitadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.UsuarioAceptante)
            .WithMany()
            .HasForeignKey(i => i.UsuarioAceptanteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.EmpresaId, i.Cedula });
    }
}
