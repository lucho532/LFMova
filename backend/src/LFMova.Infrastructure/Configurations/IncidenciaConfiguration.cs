using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LFMova.Domain.Entities;

namespace LFMova.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="Incidencia"/> y su relación con
/// <see cref="ServicioPasajero"/>.
/// </summary>
public class IncidenciaConfiguration : IEntityTypeConfiguration<Incidencia>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="Incidencia"/>.</summary>
    public void Configure(EntityTypeBuilder<Incidencia> builder)
    {
        builder.HasKey(i => i.IncidenciaId);

        builder.HasOne(i => i.ServicioPasajero)
            .WithMany(sp => sp.Incidencias)
            .HasForeignKey(i => i.ServicioPasajeroId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.ServicioPasajeroId);
    }
}
