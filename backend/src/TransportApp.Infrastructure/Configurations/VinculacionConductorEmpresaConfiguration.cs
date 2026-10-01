using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="VinculacionConductorEmpresa"/>,
/// incluyendo la restricción única <c>ConductorId + EmpresaId</c>.
/// </summary>
public class VinculacionConductorEmpresaConfiguration : IEntityTypeConfiguration<VinculacionConductorEmpresa>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="VinculacionConductorEmpresa"/>.</summary>
    public void Configure(EntityTypeBuilder<VinculacionConductorEmpresa> builder)
    {
        builder.HasKey(v => v.VinculacionConductorEmpresaId);

        builder.HasOne(v => v.Conductor)
            .WithMany(c => c.VinculacionesConductorEmpresa)
            .HasForeignKey(v => v.ConductorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Empresa)
            .WithMany(e => e.VinculacionesConductorEmpresa)
            .HasForeignKey(v => v.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.ConductorId, v.EmpresaId }).IsUnique();
    }
}
