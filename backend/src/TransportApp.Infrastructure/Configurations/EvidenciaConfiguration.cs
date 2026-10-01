using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="Evidencia"/> y su relación con
/// <see cref="Incidencia"/>.
/// </summary>
public class EvidenciaConfiguration : IEntityTypeConfiguration<Evidencia>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="Evidencia"/>.</summary>
    public void Configure(EntityTypeBuilder<Evidencia> builder)
    {
        builder.HasKey(e => e.EvidenciaId);

        builder.HasOne(e => e.Incidencia)
            .WithMany(i => i.Evidencias)
            .HasForeignKey(e => e.IncidenciaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.IncidenciaId);
    }
}
