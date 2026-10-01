using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="UnidadOperativa"/>: relación con
/// <see cref="Conductor"/> y relación 1:1 con <see cref="Vehiculo"/> (un
/// vehículo tiene como máximo una unidad operativa).
/// </summary>
public class UnidadOperativaConfiguration : IEntityTypeConfiguration<UnidadOperativa>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="UnidadOperativa"/>.</summary>
    public void Configure(EntityTypeBuilder<UnidadOperativa> builder)
    {
        builder.HasKey(u => u.UnidadOperativaId);

        builder.HasOne(u => u.Conductor)
            .WithMany(c => c.UnidadesOperativas)
            .HasForeignKey(u => u.ConductorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Vehiculo)
            .WithOne(v => v.UnidadOperativa)
            .HasForeignKey<UnidadOperativa>(u => u.VehiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.VehiculoId).IsUnique();
        builder.HasIndex(u => u.ConductorId);
    }
}
