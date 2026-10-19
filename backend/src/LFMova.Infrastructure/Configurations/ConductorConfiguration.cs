using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LFMova.Domain.Entities;

namespace LFMova.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="Conductor"/>: relación 1:0..1 con
/// <see cref="Usuario"/>.
/// </summary>
public class ConductorConfiguration : IEntityTypeConfiguration<Conductor>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="Conductor"/>.</summary>
    public void Configure(EntityTypeBuilder<Conductor> builder)
    {
        builder.HasKey(c => c.ConductorId);

        builder.HasOne(c => c.Usuario)
            .WithOne(u => u.Conductor)
            .HasForeignKey<Conductor>(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.UsuarioId).IsUnique();
    }
}
