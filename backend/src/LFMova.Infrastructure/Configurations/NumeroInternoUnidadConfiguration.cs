using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LFMova.Domain.Entities;

namespace LFMova.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="NumeroInternoUnidad"/>: un número por unidad y empresa, y un
/// mismo número no se repite dentro de una empresa. Al borrar la empresa o la unidad, el número se borra
/// con ellas.
/// </summary>
public class NumeroInternoUnidadConfiguration : IEntityTypeConfiguration<NumeroInternoUnidad>
{
    /// <summary>Longitud máxima del número interno.</summary>
    public const int LongitudMaxima = 30;

    /// <summary>Aplica la configuración de EF Core para <see cref="NumeroInternoUnidad"/>.</summary>
    public void Configure(EntityTypeBuilder<NumeroInternoUnidad> builder)
    {
        builder.HasKey(n => n.NumeroInternoUnidadId);
        builder.Property(n => n.Numero).IsRequired().HasMaxLength(LongitudMaxima);

        builder.HasOne(n => n.Empresa)
            .WithMany()
            .HasForeignKey(n => n.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.UnidadOperativa)
            .WithMany()
            .HasForeignKey(n => n.UnidadOperativaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.EmpresaId, n.UnidadOperativaId }).IsUnique();
        builder.HasIndex(n => new { n.EmpresaId, n.Numero }).IsUnique();
    }
}
