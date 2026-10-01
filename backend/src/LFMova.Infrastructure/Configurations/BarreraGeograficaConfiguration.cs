using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LFMova.Domain.Entities;

namespace LFMova.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="BarreraGeografica"/> y su relación
/// con <see cref="Empresa"/>.
/// </summary>
public class BarreraGeograficaConfiguration : IEntityTypeConfiguration<BarreraGeografica>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="BarreraGeografica"/>.</summary>
    public void Configure(EntityTypeBuilder<BarreraGeografica> builder)
    {
        builder.HasKey(b => b.BarreraGeograficaId);

        builder.HasOne(b => b.Empresa)
            .WithMany(e => e.BarrerasGeograficas)
            .HasForeignKey(b => b.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.EmpresaId);
    }
}
