using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="Vehiculo"/>: relación con
/// <see cref="Conductor"/> y unicidad de la placa.
/// </summary>
public class VehiculoConfiguration : IEntityTypeConfiguration<Vehiculo>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="Vehiculo"/>.</summary>
    public void Configure(EntityTypeBuilder<Vehiculo> builder)
    {
        builder.HasKey(v => v.VehiculoId);

        builder.Property(v => v.Placa).IsRequired();

        builder.HasIndex(v => v.Placa).IsUnique();

        builder.HasOne(v => v.Conductor)
            .WithMany(c => c.Vehiculos)
            .HasForeignKey(v => v.ConductorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => v.ConductorId);
    }
}
