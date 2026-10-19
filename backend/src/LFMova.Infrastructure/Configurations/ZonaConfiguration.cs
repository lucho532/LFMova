using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LFMova.Domain.Entities;

namespace LFMova.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="Zona"/> y su relación con
/// <see cref="Empresa"/>.
/// </summary>
public class ZonaConfiguration : IEntityTypeConfiguration<Zona>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="Zona"/>.</summary>
    public void Configure(EntityTypeBuilder<Zona> builder)
    {
        builder.HasKey(z => z.ZonaId);

        builder.HasOne(z => z.Empresa)
            .WithMany(e => e.Zonas)
            .HasForeignKey(z => z.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(z => z.MacroZona)
            .WithMany(m => m.Zonas)
            .HasForeignKey(z => z.MacroZonaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(z => z.CorredorVial)
            .WithMany(c => c.Zonas)
            .HasForeignKey(z => z.CorredorVialId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(z => z.EmpresaId);
        builder.HasIndex(z => z.MacroZonaId);
        builder.HasIndex(z => z.CorredorVialId);
    }
}
