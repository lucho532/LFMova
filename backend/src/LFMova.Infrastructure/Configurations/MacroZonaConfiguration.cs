using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LFMova.Domain.Entities;

namespace LFMova.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="MacroZona"/> y su relación con
/// <see cref="Empresa"/>.
/// </summary>
public class MacroZonaConfiguration : IEntityTypeConfiguration<MacroZona>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="MacroZona"/>.</summary>
    public void Configure(EntityTypeBuilder<MacroZona> builder)
    {
        builder.HasKey(m => m.MacroZonaId);

        builder.HasOne(m => m.Empresa)
            .WithMany(e => e.MacroZonas)
            .HasForeignKey(m => m.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.EmpresaId);
    }
}
