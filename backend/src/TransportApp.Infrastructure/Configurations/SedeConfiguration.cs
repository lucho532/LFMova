using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="Sede"/> y su relación con
/// <see cref="Empresa"/>.
/// </summary>
public class SedeConfiguration : IEntityTypeConfiguration<Sede>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="Sede"/>.</summary>
    public void Configure(EntityTypeBuilder<Sede> builder)
    {
        builder.HasKey(s => s.SedeId);

        builder.HasOne(s => s.Empresa)
            .WithMany(e => e.Sedes)
            .HasForeignKey(s => s.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.EmpresaId);
    }
}
