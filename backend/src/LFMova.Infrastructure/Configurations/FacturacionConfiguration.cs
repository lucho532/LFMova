using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LFMova.Domain.Entities;

namespace LFMova.Infrastructure.Configurations;

/// <summary>
/// Configuración de persistencia de <see cref="UsoConductor"/>: un registro
/// por ruta finalizada, sin relación con el conductor (guarda una copia de sus
/// datos) para que sobreviva a su eliminación.
/// </summary>
public class UsoConductorConfiguration : IEntityTypeConfiguration<UsoConductor>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UsoConductor> builder)
    {
        builder.ToTable("UsosConductor");
        builder.HasKey(u => u.UsoConductorId);

        builder.HasOne(u => u.Empresa)
            .WithMany()
            .HasForeignKey(u => u.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.ServicioId).IsUnique();
        builder.HasIndex(u => new { u.EmpresaId, u.Fecha });
    }
}

/// <summary>
/// Configuración de persistencia de <see cref="CierreMensual"/>: un único
/// cierre por empresa y mes.
/// </summary>
public class CierreMensualConfiguration : IEntityTypeConfiguration<CierreMensual>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CierreMensual> builder)
    {
        builder.ToTable("CierresMensuales");
        builder.HasKey(c => c.CierreMensualId);

        builder.HasOne(c => c.Empresa)
            .WithMany()
            .HasForeignKey(c => c.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.EmpresaId, c.Anio, c.Mes }).IsUnique();
    }
}
