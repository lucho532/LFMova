using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="CorredorVial"/> y su relación con
/// <see cref="Empresa"/>.
/// </summary>
public class CorredorVialConfiguration : IEntityTypeConfiguration<CorredorVial>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="CorredorVial"/>.</summary>
    public void Configure(EntityTypeBuilder<CorredorVial> builder)
    {
        builder.HasKey(c => c.CorredorVialId);

        builder.HasOne(c => c.Empresa)
            .WithMany(e => e.CorredoresViales)
            .HasForeignKey(c => c.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.EmpresaId);
    }
}
