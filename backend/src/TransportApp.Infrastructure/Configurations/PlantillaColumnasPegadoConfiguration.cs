using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="PlantillaColumnasPegado"/> y su
/// relación con <see cref="Empresa"/>. Como máximo una por empresa.
/// </summary>
public class PlantillaColumnasPegadoConfiguration : IEntityTypeConfiguration<PlantillaColumnasPegado>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="PlantillaColumnasPegado"/>.</summary>
    public void Configure(EntityTypeBuilder<PlantillaColumnasPegado> builder)
    {
        builder.HasKey(p => p.PlantillaColumnasPegadoId);

        builder.HasOne(p => p.Empresa)
            .WithMany()
            .HasForeignKey(p => p.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.EmpresaId).IsUnique();
    }
}
