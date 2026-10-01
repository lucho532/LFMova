using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="ImportacionExcel"/>: relación con
/// <see cref="Empresa"/> y con el <see cref="Usuario"/> coordinador que
/// realizó la importación.
/// </summary>
public class ImportacionExcelConfiguration : IEntityTypeConfiguration<ImportacionExcel>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="ImportacionExcel"/>.</summary>
    public void Configure(EntityTypeBuilder<ImportacionExcel> builder)
    {
        builder.HasKey(i => i.ImportacionExcelId);

        builder.HasOne(i => i.Empresa)
            .WithMany(e => e.ImportacionesExcel)
            .HasForeignKey(i => i.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Coordinador)
            .WithMany()
            .HasForeignKey(i => i.CoordinadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.EmpresaId);
    }
}
