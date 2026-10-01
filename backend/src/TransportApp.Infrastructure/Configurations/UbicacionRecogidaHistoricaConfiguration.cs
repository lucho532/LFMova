using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="UbicacionRecogidaHistorica"/> y su
/// relación con <see cref="Empleado"/>.
/// </summary>
public class UbicacionRecogidaHistoricaConfiguration : IEntityTypeConfiguration<UbicacionRecogidaHistorica>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="UbicacionRecogidaHistorica"/>.</summary>
    public void Configure(EntityTypeBuilder<UbicacionRecogidaHistorica> builder)
    {
        builder.HasKey(u => u.UbicacionRecogidaHistoricaId);

        builder.HasOne(u => u.Empleado)
            .WithMany(e => e.UbicacionesRecogidaHistorica)
            .HasForeignKey(u => u.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.EmpleadoId);
    }
}
